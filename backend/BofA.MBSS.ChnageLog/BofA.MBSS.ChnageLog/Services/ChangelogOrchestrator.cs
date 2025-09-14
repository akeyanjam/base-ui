using BofA.MBSS.ChnageLog.Models;
using BofA.MBSS.ChnageLog.Services.Bitbucket;
using BofA.MBSS.ChnageLog.Services.Jira;

namespace BofA.MBSS.ChnageLog.Services;

public interface IChangelogOrchestrator
{
    Task<BuildReportResponse> GenerateReportAsync(
        BuildReportRequest request,
        CancellationToken cancellationToken = default);
}

public class ChangelogOrchestrator : IChangelogOrchestrator
{
    private readonly IBitbucketService _bitbucketService;
    private readonly IJiraService _jiraService;
    private readonly ILogger<ChangelogOrchestrator> _logger;

    public ChangelogOrchestrator(
        IBitbucketService bitbucketService,
        IJiraService jiraService,
        ILogger<ChangelogOrchestrator> logger)
    {
        _bitbucketService = bitbucketService;
        _jiraService = jiraService;
        _logger = logger;
    }

    public async Task<BuildReportResponse> GenerateReportAsync(
        BuildReportRequest request,
        CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["ReleaseBranch"] = request.ReleaseBranch,
            ["FromDate"] = request.From,
            ["ToDate"] = request.To,
            ["RepoCount"] = request.Repos.Count
        });

        _logger.LogInformation("Starting changelog generation for {ReleaseBranch}", request.ReleaseBranch);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // 1. Validate inputs
            ValidateRequest(request);

            // 2. Discover PRs from all repos in parallel
            _logger.LogDebug("Fetching PRs from {RepoCount} repositories", request.Repos.Count);
            
            var prTasks = request.Repos.Select(async repo =>
            {
                var prs = await _bitbucketService.GetMergedPullRequestsAsync(
                    repo, request.ReleaseBranch, request.From, request.To, cancellationToken);
                return new { Repo = repo, PullRequests = prs };
            });

            var prResults = await Task.WhenAll(prTasks);

            // 3. Extract all unique Jira keys from source branch names
            _logger.LogDebug("Extracting Jira keys from branch names");
            
            var allJiraKeys = new HashSet<string>();
            foreach (var result in prResults)
            {
                foreach (var pr in result.PullRequests)
                {
                    var keys = _bitbucketService.ExtractJiraKeys(pr.SourceBranch);
                    foreach (var key in keys)
                    {
                        allJiraKeys.Add(key);
                    }
                }
            }

            _logger.LogDebug("Found {KeyCount} unique Jira keys: {Keys}", 
                allJiraKeys.Count, string.Join(", ", allJiraKeys.Take(10)) + (allJiraKeys.Count > 10 ? "..." : ""));

            // 4. Fetch all Jira issues in parallel
            var jiraIssues = await _jiraService.GetIssuesBatchAsync(allJiraKeys, cancellationToken);

            // 5. Build story mappings
            _logger.LogDebug("Building stories from PRs and Jira issues");
            
            var stories = new List<StoryInfo>();
            foreach (var result in prResults)
            {
                var repoStories = await BuildStoriesForRepo(result.Repo, result.PullRequests, jiraIssues, cancellationToken);
                stories.AddRange(repoStories);
            }

            // 6. Generate summary
            var summary = BuildSummary(stories, request.Repos);

            var response = new BuildReportResponse(
                request.ReleaseBranch,
                request.From,
                request.To,
                DateTimeOffset.UtcNow,
                stories,
                summary);

            _logger.LogInformation("Changelog generation completed in {ElapsedMs}ms. Found {StoryCount} stories",
                stopwatch.ElapsedMilliseconds, stories.Count);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Changelog generation failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private void ValidateRequest(BuildReportRequest request)
    {
        if (!request.Repos.Any())
            throw new ArgumentException("At least one repository must be specified");

        if (request.Repos.Count > 10)
            throw new ArgumentException("Maximum 10 repositories allowed");

        if (string.IsNullOrWhiteSpace(request.ReleaseBranch))
            throw new ArgumentException("Release branch is required");

        if (!System.Text.RegularExpressions.Regex.IsMatch(request.ReleaseBranch, @"^release/\d{4}-\d{2}$"))
            throw new ArgumentException("Release branch must match format 'release/YYYY-MM'");

        if (request.From >= request.To)
            throw new ArgumentException("From date must be before To date");

        if ((request.To - request.From).TotalDays > 90)
            throw new ArgumentException("Date range cannot exceed 90 days");
    }

    private Task<IReadOnlyList<StoryInfo>> BuildStoriesForRepo(
        RepoRef repo,
        IReadOnlyList<PullRequestInfo> pullRequests,
        Dictionary<string, JiraIssueInfo> jiraIssues,
        CancellationToken cancellationToken)
    {
        var stories = new List<StoryInfo>();
        var storyToPrs = new Dictionary<string, List<PullRequestInfo>>();
        var storyToBranches = new Dictionary<string, HashSet<string>>();

        // Group PRs by Jira keys
        foreach (var pr in pullRequests)
        {
            var jiraKeys = _bitbucketService.ExtractJiraKeys(pr.SourceBranch);
            
            foreach (var key in jiraKeys)
            {
                if (!storyToPrs.ContainsKey(key))
                {
                    storyToPrs[key] = new List<PullRequestInfo>();
                    storyToBranches[key] = new HashSet<string>();
                }
                
                storyToPrs[key].Add(pr);
                storyToBranches[key].Add(pr.SourceBranch);
            }
        }

        // Build stories
        foreach (var (jiraKey, prs) in storyToPrs)
        {
            if (jiraIssues.TryGetValue(jiraKey, out var jiraIssue))
            {
                var story = new StoryInfo(
                    jiraIssue,
                    repo,
                    storyToBranches[jiraKey].ToList(),
                    prs);
                    
                stories.Add(story);
            }
            else
            {
                _logger.LogWarning("Jira issue {JiraKey} not found for repo {ProjectKey}/{Slug}", 
                    jiraKey, repo.ProjectKey, repo.Slug);
            }
        }

        return Task.FromResult<IReadOnlyList<StoryInfo>>(stories);
    }

    private ReportSummary BuildSummary(IReadOnlyList<StoryInfo> stories, IReadOnlyList<RepoRef> repos)
    {
        var repoBreakdown = new Dictionary<string, int>();
        var statusBreakdown = new Dictionary<string, int>();

        // Initialize repo breakdown with zero counts
        foreach (var repo in repos)
        {
            repoBreakdown[$"{repo.ProjectKey}/{repo.Slug}"] = 0;
        }

        foreach (var story in stories)
        {
            // Update repo breakdown
            var repoKey = $"{story.Repo.ProjectKey}/{story.Repo.Slug}";
            repoBreakdown[repoKey] = repoBreakdown.GetValueOrDefault(repoKey, 0) + 1;

            // Update status breakdown
            statusBreakdown[story.Issue.Status] = statusBreakdown.GetValueOrDefault(story.Issue.Status, 0) + 1;
        }

        return new ReportSummary(stories.Count, repoBreakdown, statusBreakdown);
    }
}
