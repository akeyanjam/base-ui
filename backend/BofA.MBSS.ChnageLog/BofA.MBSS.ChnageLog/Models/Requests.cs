using System.ComponentModel.DataAnnotations;

namespace BofA.MBSS.ChnageLog.Models;

// API Request Models
public record BuildReportRequest(
    [Required] IReadOnlyList<RepoRef> Repos,
    [Required] string ReleaseBranch,
    [Required] DateTimeOffset From,
    [Required] DateTimeOffset To);
