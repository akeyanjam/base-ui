using System.Text.Json;
using System.Text.RegularExpressions;
using BofA.MBSS.ChnageLog.Models;
using BofA.MBSS.ChnageLog.Services.Bitbucket.Dtos;

namespace BofA.MBSS.ChnageLog.Services.Bitbucket;

public interface IBitbucketService
{
    Task<IReadOnlyList<PullRequestInfo>> GetMergedPullRequestsAsync(
        RepoRef repo,
        string releaseBranch,
        DateTimeOffset fromDate,
        DateTimeOffset toDate,
        CancellationToken cancellationToken = default);
        
    IReadOnlyList<string> ExtractJiraKeys(string branchName);
}

public class BitbucketService : IBitbucketService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BitbucketService> _logger;
    private readonly Regex _jiraKeyRegex = new(@"\b(?:MBCS|MBBO|MBOS|MBSS)-\d+\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public BitbucketService(HttpClient httpClient, ILogger<BitbucketService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PullRequestInfo>> GetMergedPullRequestsAsync(
        RepoRef repo,
        string releaseBranch,
        DateTimeOffset fromDate,
        DateTimeOffset toDate,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching merged PRs for {ProjectKey}/{Slug} to {ReleaseBranch} between {From} and {To}",
            repo.ProjectKey, repo.Slug, releaseBranch, fromDate, toDate);

        var pullRequests = new List<PullRequestInfo>();
        var start = 0;
        const int limit = 50;

        try
        {
            while (true)
            {
                var url = $"/rest/api/1.0/projects/{repo.ProjectKey}/repos/{repo.Slug}/pull-requests" +
                          $"?state=MERGED&start={start}&limit={limit}";

                _logger.LogDebug("Calling Bitbucket API: {Url}", url);

                var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var page = JsonSerializer.Deserialize<BitbucketPage<BitbucketPullRequest>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }) ?? throw new InvalidOperationException("Failed to deserialize Bitbucket response");

                foreach (var pr in page.Values)
                {
                    // Filter by target branch and merge date
                    if (pr.ToRef.DisplayId.EndsWith($"/{releaseBranch}") &&
                        pr.UpdatedDate.HasValue &&
                        pr.UpdatedDate >= fromDate &&
                        pr.UpdatedDate <= toDate)
                    {
                        var prInfo = new PullRequestInfo(
                            pr.Id,
                            pr.Title,
                            pr.FromRef.DisplayId,
                            pr.ToRef.DisplayId,
                            pr.UpdatedDate.Value,
                            pr.Author?.User?.EmailAddress,
                            new Uri(pr.Links.Self.FirstOrDefault()?.Href ?? $"{_httpClient.BaseAddress}projects/{repo.ProjectKey}/repos/{repo.Slug}/pull-requests/{pr.Id}")
                        );

                        pullRequests.Add(prInfo);
                    }
                }

                if (page.IsLastPage || page.Values.Count == 0)
                    break;

                start += limit;
            }

            _logger.LogInformation("Found {Count} merged PRs for {ProjectKey}/{Slug}", pullRequests.Count, repo.ProjectKey, repo.Slug);
            return pullRequests;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch PRs from Bitbucket for {ProjectKey}/{Slug}", repo.ProjectKey, repo.Slug);
            throw;
        }
    }

    public IReadOnlyList<string> ExtractJiraKeys(string branchName)
    {
        // Only process branches with allowed prefixes
        if (!branchName.StartsWith("feature/") && !branchName.StartsWith("bugfix/"))
            return Array.Empty<string>();

        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Split branch name and check second token first (after prefix)
        var parts = branchName.Split('/');
        if (parts.Length >= 2)
        {
            var matches = _jiraKeyRegex.Matches(parts[1]);
            foreach (Match match in matches)
            {
                keys.Add(match.Value.ToUpperInvariant());
            }
        }

        // If no keys found in second token, check entire branch name
        if (keys.Count == 0)
        {
            var matches = _jiraKeyRegex.Matches(branchName);
            foreach (Match match in matches)
            {
                keys.Add(match.Value.ToUpperInvariant());
            }
        }

        return keys.ToList();
    }
}
