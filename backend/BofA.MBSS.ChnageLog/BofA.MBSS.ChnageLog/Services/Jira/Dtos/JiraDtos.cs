using System.Text.Json.Serialization;

namespace BofA.MBSS.ChnageLog.Services.Jira.Dtos;

// Jira API Response Models
public record JiraIssue(
    string Key,
    JiraIssueFields Fields,
    string Self);

public record JiraIssueFields(
    string Summary,
    JiraStatus Status,
    JiraUser? Assignee,
    string? Description)
{
    [JsonPropertyName("customfield_10371")]
    public string? EpicLink { get; init; }

    [JsonPropertyName("customfield_12272")]
    public string? PmEpicLink { get; init; }
};

public record JiraStatus(
    string Name,
    string Description);

public record JiraUser(
    string DisplayName,
    string EmailAddress,
    string AccountId);


public record JiraSearchResponse(
    List<JiraIssue> Issues,
    int Total,
    int StartAt,
    int MaxResults);

public record JiraSearchRequest(
    string Jql,
    List<string> Fields,
    int StartAt = 0,
    int MaxResults = 50);
