namespace BofA.MBSS.ChnageLog.Models;

// API Response Models
public record BuildReportResponse(
    string ReleaseBranch,
    DateTimeOffset From,
    DateTimeOffset To,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<StoryInfo> Stories,
    ReportSummary Summary);

public record RepoListResponse(
    IReadOnlyList<RepoRef> Repos,
    string DefaultReleaseBranch);

public record HealthResponse(
    string Status,
    DateTimeOffset Timestamp,
    string Version);
