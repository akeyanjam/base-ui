namespace BofA.MBSS.ChnageLog.Models;

// Core Domain Models
public record RepoRef(string ProjectKey, string Slug);

public record StoryInfo(
    JiraIssueInfo Issue,
    RepoRef Repo,
    IReadOnlyList<string> SourceBranches,
    IReadOnlyList<PullRequestInfo> PullRequests);

public record JiraIssueInfo(
    string Key,
    string Summary,
    string Status,
    string? Assignee,
    Uri Url,
    IReadOnlyList<RelatedEpic> RelatedEpics);

public record RelatedEpic(
    string Key,
    string? Summary,
    Uri? Url,
    string Relationship); // "required by" for main epic, "related" for PM epic

public record PullRequestInfo(
    int Id,
    string Title,
    string SourceBranch,
    string TargetBranch,
    DateTimeOffset MergedAt,
    string? MergedBy,
    Uri Url);

public record ReportSummary(
    int TotalStories,
    Dictionary<string, int> RepoBreakdown,
    Dictionary<string, int> StatusBreakdown);
