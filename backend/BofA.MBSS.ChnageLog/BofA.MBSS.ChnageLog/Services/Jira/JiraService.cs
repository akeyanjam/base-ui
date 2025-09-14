using System.Text;
using System.Text.Json;
using BofA.MBSS.ChnageLog.Models;
using BofA.MBSS.ChnageLog.Services.Jira.Dtos;

namespace BofA.MBSS.ChnageLog.Services.Jira;

public interface IJiraService
{
    Task<Dictionary<string, JiraIssueInfo>> GetIssuesBatchAsync(
        IEnumerable<string> issueKeys,
        CancellationToken cancellationToken = default);
}

public class JiraService : IJiraService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<JiraService> _logger;
    private readonly IRequestScopedCache _cache;
    private readonly string _epicLinkFieldName;
    private readonly string _pmEpicFieldName;

    public JiraService(
        HttpClient httpClient, 
        ILogger<JiraService> logger, 
        IRequestScopedCache cache,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _cache = cache;
        _epicLinkFieldName = configuration["Jira:EpicLinkFieldName"] ?? "customfield_10371";
        _pmEpicFieldName = configuration["Jira:PmEpicFieldName"] ?? "customfield_12272";
    }

    public async Task<Dictionary<string, JiraIssueInfo>> GetIssuesBatchAsync(
        IEnumerable<string> issueKeys,
        CancellationToken cancellationToken = default)
    {
        var keys = issueKeys.ToList();
        var result = new Dictionary<string, JiraIssueInfo>();
        var uncachedKeys = new List<string>();

        // Check cache first
        foreach (var key in keys)
        {
            if (_cache.TryGet(key, out var cachedIssue) && cachedIssue != null)
            {
                result[key] = cachedIssue;
            }
            else
            {
                uncachedKeys.Add(key);
            }
        }

        // Fetch uncached issues
        if (uncachedKeys.Any())
        {
            _logger.LogDebug("Fetching {Count} uncached issues from Jira: {Keys}", uncachedKeys.Count, string.Join(", ", uncachedKeys));

            try
            {
                var freshIssues = await FetchIssuesFromApi(uncachedKeys, cancellationToken);

                foreach (var (key, issue) in freshIssues)
                {
                    _cache.Set(key, issue);
                    result[key] = issue;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch issues from Jira API");
                throw;
            }
        }

        _logger.LogInformation("Retrieved {Count} Jira issues ({CachedCount} from cache, {FreshCount} from API)", 
            result.Count, keys.Count - uncachedKeys.Count, uncachedKeys.Count);

        return result;
    }

    private async Task<Dictionary<string, JiraIssueInfo>> FetchIssuesFromApi(
        List<string> issueKeys,
        CancellationToken cancellationToken)
    {
        var jql = $"key IN ({string.Join(",", issueKeys.Select(k => $"'{k}'"))})";
        var searchRequest = new JiraSearchRequest(
            jql,
            new List<string> { "summary", "status", "assignee", "description", _epicLinkFieldName },
            0,
            issueKeys.Count
        );

        var jsonContent = JsonSerializer.Serialize(searchRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("/rest/api/2/search", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var searchResponse = JsonSerializer.Deserialize<JiraSearchResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }) ?? throw new InvalidOperationException("Failed to deserialize Jira search response");

        var result = new Dictionary<string, JiraIssueInfo>();

        foreach (var issue in searchResponse.Issues)
        {
            var relatedEpics = await ResolveEpicsAsync(issue, cancellationToken);
            var issueInfo = new JiraIssueInfo(
                issue.Key,
                issue.Fields.Summary,
                issue.Fields.Status.Name,
                issue.Fields.Assignee?.DisplayName,
                new Uri($"{_httpClient.BaseAddress}browse/{issue.Key}"),
                relatedEpics
            );

            result[issue.Key] = issueInfo;
        }

        return result;
    }

    private async Task<IReadOnlyList<RelatedEpic>> ResolveEpicsAsync(
        JiraIssue issue,
        CancellationToken cancellationToken)
    {
        var relatedEpics = new List<RelatedEpic>();

        // 1. Add main Epic Link (direct parent epic) - marked as "required by"
        if (!string.IsNullOrEmpty(issue.Fields.EpicLink))
        {
            var epic = await FetchEpicWithPmEpic(issue.Fields.EpicLink, cancellationToken);
            if (epic != null)
            {
                relatedEpics.Add(new RelatedEpic(
                    issue.Fields.EpicLink,
                    epic.Fields.Summary,
                    new Uri($"{_httpClient.BaseAddress}browse/{issue.Fields.EpicLink}"),
                    "required by"));

                // 2. Add PM Epic from the main epic's PM epic field - marked as "related"
                if (!string.IsNullOrEmpty(epic.Fields.PmEpicLink))
                {
                    var pmEpic = await FetchEpicSummary(epic.Fields.PmEpicLink, cancellationToken);
                    if (pmEpic != null)
                    {
                        relatedEpics.Add(new RelatedEpic(
                            epic.Fields.PmEpicLink,
                            pmEpic.Fields.Summary,
                            new Uri($"{_httpClient.BaseAddress}browse/{epic.Fields.PmEpicLink}"),
                            "related"));
                    }
                }
            }
        }

        return relatedEpics;
    }

    private async Task<JiraIssue?> FetchEpicWithPmEpic(string epicKey, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/rest/api/2/issue/{epicKey}?fields=summary,{_pmEpicFieldName}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch epic {EpicKey}: {StatusCode}", epicKey, response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<JiraIssue>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching epic with PM epic for {EpicKey}", epicKey);
            return null;
        }
    }

    private async Task<JiraIssue?> FetchEpicSummary(string epicKey, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/rest/api/2/issue/{epicKey}?fields=summary", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch epic {EpicKey}: {StatusCode}", epicKey, response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<JiraIssue>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching epic summary for {EpicKey}", epicKey);
            return null;
        }
    }
}
