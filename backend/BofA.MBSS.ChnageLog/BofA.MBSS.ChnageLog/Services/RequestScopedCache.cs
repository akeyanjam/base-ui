using BofA.MBSS.ChnageLog.Models;

namespace BofA.MBSS.ChnageLog.Services;

// Simple request-scoped cache using Dictionary
public interface IRequestScopedCache
{
    void Set(string storyId, JiraIssueInfo issue);
    JiraIssueInfo? Get(string storyId);
    bool TryGet(string storyId, out JiraIssueInfo? issue);
}

public class RequestScopedCache : IRequestScopedCache
{
    private readonly Dictionary<string, JiraIssueInfo> _cache = new();

    public void Set(string storyId, JiraIssueInfo issue)
    {
        _cache[storyId] = issue;
    }

    public JiraIssueInfo? Get(string storyId)
    {
        return _cache.GetValueOrDefault(storyId);
    }

    public bool TryGet(string storyId, out JiraIssueInfo? issue)
    {
        return _cache.TryGetValue(storyId, out issue);
    }
}
