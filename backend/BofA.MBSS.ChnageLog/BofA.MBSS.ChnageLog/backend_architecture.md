# SIT Changelog Builder - Backend Architecture Document
**(.NET 8 Web API + Bitbucket DC + Jira DC)**

---

## 1. System Overview

### 1.1 Purpose
Automated changelog generation tool that discovers Jira stories delivered to a release branch within a specified date range by querying Bitbucket Data Center pull requests and enriching with Jira Data Center issue information.

### 1.2 High-Level Architecture
```
┌─────────────────────────────────────────────────────┐
│                Vue 3 SPA Frontend                   │
│            (Vite + Tailwind + shadcn-vue)          │
└─────────────────┬───────────────────────────────────┘
                  │ HTTP/JSON
┌─────────────────▼───────────────────────────────────┐
│              ASP.NET Core 8 Web API                 │
│  ┌─────────────────────────────────────────────────┐│
│  │           Report Controller                     ││
│  └─────────────────┬───────────────────────────────┘│
│  ┌─────────────────▼───────────────────────────────┐│
│  │        Changelog Orchestrator Service          ││
│  └─┬─────────────────────────────────────────────┬─┘│
│    │                                             │  │
│  ┌─▼──────────────────┐  ┌──────────────────────▼─┐│
│  │  Bitbucket Service │  │     Jira Service       ││
│  └─┬──────────────────┘  └──────────────────────┬─┘│
│    │                                             │  │
│  ┌─▼──────────────────┐  ┌──────────────────────▼─┐│
│  │  Branch Parser     │  │   Epic Resolver        ││
│  └────────────────────┘  └────────────────────────┘│
└─────────────────┬───────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────┐
│     External Systems (via HTTPS + Basic Auth)      │
│  ┌─────────────────────┐  ┌─────────────────────────┐│
│  │   Bitbucket DC      │  │      Jira DC            ││
│  │   REST API 1.0      │  │   REST API 2.0          ││
│  └─────────────────────┘  └─────────────────────────┘│
└─────────────────────────────────────────────────────┘
```

### 1.3 Key Design Decisions
- **Direct PR Query Approach**: Query merged PRs directly rather than commit-first discovery
- **Structured Data Response**: Backend returns organized data; frontend handles markdown rendering
- **Fail-Fast Strategy**: No partial results; fail on any API errors
- **Service Account Authentication**: Backend holds all API credentials
- **Memory Caching**: In-process caching for Jira issues and Epic field metadata

---

## 2. API Surface

### 2.1 Endpoints Overview
```
Base URL: /api

GET  /api/meta/repos          - Get available repositories
POST /api/report/build        - Generate changelog report  
GET  /api/health              - Health check
```

### 2.2 Detailed Endpoint Specifications

#### 2.2.1 Repository Metadata
```http
GET /api/meta/repos
```

**Response (200 OK):**
```json
{
  "repos": [
    { "projectKey": "PAY", "slug": "payments-api" },
    { "projectKey": "PAY", "slug": "settlements-engine" },  
    { "projectKey": "CORE", "slug": "platform-services" }
  ],
  "defaultReleaseBranch": "release/2025-09"
}
```

#### 2.2.2 Report Generation
```http
POST /api/report/build
Content-Type: application/json
```

**Request:**
```json
{
  "repos": [
    { "projectKey": "PAY", "slug": "payments-api" },
    { "projectKey": "PAY", "slug": "settlements-engine" }
  ],
  "releaseBranch": "release/2025-09",
  "from": "2025-08-20T00:00:00.000Z",
  "to": "2025-09-13T23:59:59.999Z"
}
```

**Response (200 OK):**
```json
{
  "releaseBranch": "release/2025-09",
  "from": "2025-08-20T00:00:00.000Z", 
  "to": "2025-09-13T23:59:59.999Z",
  "generatedAt": "2025-09-13T15:30:45.123Z",
  "stories": [
    {
      "issue": {
        "key": "MBBO-1234",
        "summary": "Enable multi-bank payment routing",
        "status": "In QA",
        "assignee": "John Smith",
        "url": "https://jira.company.com/browse/MBBO-1234",
        "relatedEpics": [
          {
            "key": "PAY-EP-45",
            "summary": "Payment Gateway Redesign", 
            "url": "https://jira.company.com/browse/PAY-EP-45",
            "relationship": "Epic Link"
          },
          {
            "key": "MERCH-89",
            "summary": "Q4 Commerce Platform",
            "url": "https://jira.company.com/browse/MERCH-89", 
            "relationship": "Required by"
          }
        ]
      },
      "repo": { "projectKey": "PAY", "slug": "payments-api" },
      "sourceBranches": ["feature/MBBO-1234-multi-bank-routing"],
      "pullRequests": [
        {
          "id": 147,
          "title": "MBBO-1234: Implement multi-bank routing logic",
          "sourceBranch": "feature/MBBO-1234-multi-bank-routing",
          "targetBranch": "release/2025-09",
          "mergedAt": "2025-09-10T14:22:33.000Z",
          "mergedBy": "jane.doe@company.com",
          "url": "https://bitbucket.company.com/projects/PAY/repos/payments-api/pull-requests/147"
        }
      ]
    }
  ],
  "summary": {
    "totalStories": 12,
    "repoBreakdown": {
      "PAY/payments-api": 8,
      "PAY/settlements-engine": 4
    },
    "statusBreakdown": {
      "Done": 7,
      "In QA": 3,
      "In Progress": 2
    }
  }
}
```

**Error Response (400 Bad Request):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request", 
  "status": 400,
  "detail": "Release branch must match pattern 'release/YYYY-MM'",
  "errors": {
    "releaseBranch": ["Invalid format. Expected 'release/YYYY-MM'."]
  }
}
```

#### 2.2.3 Health Check
```http
GET /api/health
```

**Response (200 OK):**
```json
{
  "status": "Healthy",
  "timestamp": "2025-09-13T15:30:45.123Z",
  "version": "1.0.0"
}
```

---

## 3. Core Services Architecture

### 3.1 Service Interfaces

```csharp
public interface IChangelogOrchestrator
{
    Task<BuildReportResponse> GenerateReportAsync(
        BuildReportRequest request, 
        CancellationToken cancellationToken = default);
}

public interface IBitbucketService  
{
    Task<IReadOnlyList<BitbucketPullRequest>> GetMergedPullRequestsAsync(
        RepoRef repo, 
        string releaseBranch, 
        DateTimeOffset fromDate, 
        DateTimeOffset toDate,
        CancellationToken cancellationToken = default);
}

public interface IJiraService
{
    Task<Dictionary<string, JiraIssue>> GetIssuesBatchAsync(
        IEnumerable<string> issueKeys,
        CancellationToken cancellationToken = default);
        
    Task<string?> GetEpicLinkFieldIdAsync(
        CancellationToken cancellationToken = default);
}

public interface IBranchKeyExtractor
{
    IReadOnlyList<string> ExtractJiraKeys(string branchName);
}

public interface IEpicResolver
{
    Task<IReadOnlyList<RelatedEpic>> ResolveEpicsAsync(
        JiraIssue issue,
        CancellationToken cancellationToken = default);
}
```

### 3.2 Core Data Models

```csharp
// API Request/Response Models
public record BuildReportRequest(
    IReadOnlyList<RepoRef> Repos,
    string ReleaseBranch,
    DateTimeOffset From,
    DateTimeOffset To);

public record BuildReportResponse(
    string ReleaseBranch,
    DateTimeOffset From,
    DateTimeOffset To,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<StoryInfo> Stories,
    ReportSummary Summary);

public record RepoRef(string ProjectKey, string Slug);

// Domain Models
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
    string Relationship); // "Epic Link", "Required by", "Relates"

public record PullRequestInfo(
    int Id,
    string Title,
    string SourceBranch,
    string TargetBranch,
    DateTimeOffset MergedAt,
    string? MergedBy,
    Uri Url);

// Internal Service Models  
public record BitbucketPullRequest(
    int Id,
    string Title,
    string SourceBranch,
    string TargetBranch, 
    DateTimeOffset? MergedAt,
    string? MergedByEmail,
    Uri Url,
    string State);

public record JiraIssue(
    string Key,
    string Summary,
    string Status,
    string? AssigneeDisplayName,
    Uri BrowseUrl,
    string? EpicKey,
    IReadOnlyList<JiraIssueLink> IssueLinks,
    string? Description);

public record JiraIssueLink(
    string LinkType,
    JiraLinkedIssue InwardIssue,
    JiraLinkedIssue OutwardIssue);

public record JiraLinkedIssue(
    string Key,
    string? Summary,
    string? ProjectKey,
    string? IssueType);
```

---

## 4. Core Algorithm & Data Flow

### 4.1 Report Generation Flow
```csharp
public async Task<BuildReportResponse> GenerateReportAsync(
    BuildReportRequest request, 
    CancellationToken cancellationToken = default)
{
    // 1. Validate inputs
    ValidateRequest(request);
    
    // 2. Discover PRs from all repos in parallel
    var prTasks = request.Repos.Select(repo => 
        _bitbucketService.GetMergedPullRequestsAsync(
            repo, request.ReleaseBranch, request.From, request.To, cancellationToken));
    
    var prResults = await Task.WhenAll(prTasks);
    
    // 3. Extract all unique Jira keys from source branch names
    var allKeys = new HashSet<string>();
    foreach (var prs in prResults)
    {
        foreach (var pr in prs)
        {
            var keys = _branchKeyExtractor.ExtractJiraKeys(pr.SourceBranch);
            allKeys.UnionWith(keys);
        }
    }
    
    // 4. Fetch all Jira issues in parallel
    var jiraIssues = await _jiraService.GetIssuesBatchAsync(allKeys, cancellationToken);
    
    // 5. Build story mappings
    var stories = new List<StoryInfo>();
    for (int i = 0; i < request.Repos.Count; i++)
    {
        var repo = request.Repos[i];
        var repoPrs = prResults[i];
        
        stories.AddRange(await BuildStoriesForRepo(repo, repoPrs, jiraIssues, cancellationToken));
    }
    
    // 6. Generate summary
    var summary = BuildSummary(stories);
    
    return new BuildReportResponse(
        request.ReleaseBranch,
        request.From, 
        request.To,
        DateTimeOffset.UtcNow,
        stories,
        summary);
}
```

### 4.2 Bitbucket PR Discovery
```csharp
public async Task<IReadOnlyList<BitbucketPullRequest>> GetMergedPullRequestsAsync(
    RepoRef repo,
    string releaseBranch, 
    DateTimeOffset fromDate,
    DateTimeOffset toDate,
    CancellationToken cancellationToken = default)
{
    var prs = new List<BitbucketPullRequest>();
    var start = 0;
    const int limit = 50;
    
    while (true)
    {
        var url = $"/rest/api/1.0/projects/{repo.ProjectKey}/repos/{repo.Slug}/pull-requests" +
                  $"?state=MERGED&start={start}&limit={limit}";
                  
        var response = await _httpClient.GetAsync(url, cancellationToken);
        var page = await response.Content.ReadFromJsonAsync<BitbucketPage<BitbucketPullRequest>>(cancellationToken);
        
        foreach (var pr in page.Values)
        {
            // Filter by target branch and merge date
            if (pr.TargetBranch.EndsWith($"/{releaseBranch}") && 
                pr.MergedAt.HasValue &&
                pr.MergedAt >= fromDate && 
                pr.MergedAt <= toDate)
            {
                prs.Add(pr);
            }
        }
        
        if (page.IsLastPage || page.Values.Count == 0) break;
        start += limit;
    }
    
    return prs;
}
```

### 4.3 Branch Key Extraction
```csharp
public IReadOnlyList<string> ExtractJiraKeys(string branchName)
{
    // Only process branches with allowed prefixes
    if (!branchName.StartsWith("feature/") && !branchName.StartsWith("bugfix/"))
        return Array.Empty<string>();
    
    var keys = new HashSet<string>();
    var regex = new Regex(@"\b(?:MBCS|MBBO|MBOS|MBSS)-\d+\b", RegexOptions.IgnoreCase);
    
    // Split branch name and check second token first (after prefix)
    var parts = branchName.Split('/');
    if (parts.Length >= 2)
    {
        var matches = regex.Matches(parts[1]);
        foreach (Match match in matches)
        {
            keys.Add(match.Value.ToUpperInvariant());
        }
    }
    
    // If no keys found in second token, check entire branch name
    if (keys.Count == 0)
    {
        var matches = regex.Matches(branchName);
        foreach (Match match in matches)
        {
            keys.Add(match.Value.ToUpperInvariant());
        }
    }
    
    return keys.ToList();
}
```

### 4.4 Epic Resolution Strategy
```csharp
public async Task<IReadOnlyList<RelatedEpic>> ResolveEpicsAsync(
    JiraIssue issue,
    CancellationToken cancellationToken = default)
{
    var relatedEpics = new List<RelatedEpic>();
    
    // 1. Add Epic Link (direct parent epic)
    if (!string.IsNullOrEmpty(issue.EpicKey))
    {
        var epic = await FetchEpicSummary(issue.EpicKey, cancellationToken);
        if (epic != null)
        {
            relatedEpics.Add(new RelatedEpic(
                issue.EpicKey,
                epic.Summary,
                BuildJiraUrl(issue.EpicKey),
                "Epic Link"));
        }
    }
    
    // 2. Find PM Epics via issue links
    foreach (var link in issue.IssueLinks)
    {
        var linkedIssue = link.LinkType == "Relates" ? link.OutwardIssue : link.InwardIssue;
        
        if (linkedIssue.ProjectKey == "MERCH" && 
            linkedIssue.IssueType == "Epic" &&
            _config.PmEpicLinkTypes.Contains(link.LinkType))
        {
            relatedEpics.Add(new RelatedEpic(
                linkedIssue.Key,
                linkedIssue.Summary,
                BuildJiraUrl(linkedIssue.Key),
                link.LinkType));
        }
    }
    
    return relatedEpics;
}
```

---

## 5. Configuration & Settings

### 5.1 Application Configuration
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Changelog": "Debug"
    }
  },
  
  "Bitbucket": {
    "BaseUrl": "https://bitbucket.company.internal",
    "Username": "svc-changelog",
    "Password": "***", // From environment or KeyVault
    "TimeoutSeconds": 30
  },
  
  "Jira": {
    "BaseUrl": "https://jira.company.internal", 
    "Username": "svc-changelog",
    "Password": "***", // From environment or KeyVault  
    "TimeoutSeconds": 30,
    "EpicLinkFieldName": "Epic Link",
    "PmEpic": {
      "ProjectKey": "MERCH",
      "LinkTypes": ["Relates", "Required by"]
    }
  },
  
  "Repositories": [
    { "ProjectKey": "PAY", "Slug": "payments-api" },
    { "ProjectKey": "PAY", "Slug": "settlements-engine" },
    { "ProjectKey": "CORE", "Slug": "platform-services" }
  ],
  
  "BranchParser": {
    "AllowedPrefixes": ["feature", "bugfix"],
    "JiraProjects": ["MBCS", "MBBO", "MBOS", "MBSS"]
  },
  
  "Validation": {
    "MaxDateRangeDays": 90,
    "MaxReposPerRequest": 10
  },
  
  "Caching": {
    "JiraIssueExpiryMinutes": 15,
    "EpicFieldIdExpiryHours": 24
  }
}
```

### 5.2 Dependency Injection Setup
```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Configuration
    services.Configure<BitbucketConfig>(Configuration.GetSection("Bitbucket"));
    services.Configure<JiraConfig>(Configuration.GetSection("Jira"));
    services.Configure<RepositoryConfig>(Configuration.GetSection("Repositories"));
    
    // HTTP Clients
    services.AddHttpClient<IBitbucketService, BitbucketService>()
        .ConfigureHttpClient((provider, client) =>
        {
            var config = provider.GetRequiredService<IOptions<BitbucketConfig>>().Value;
            client.BaseAddress = new Uri(config.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
            
            var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{config.Username}:{config.Password}"));
            client.DefaultRequestHeaders.Authorization = new("Basic", auth);
        });
    
    services.AddHttpClient<IJiraService, JiraService>()
        .ConfigureHttpClient((provider, client) =>
        {
            var config = provider.GetRequiredService<IOptions<JiraConfig>>().Value;
            client.BaseAddress = new Uri(config.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
            
            var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{config.Username}:{config.Password}"));
            client.DefaultRequestHeaders.Authorization = new("Basic", auth);
        });
    
    // Core Services
    services.AddScoped<IChangelogOrchestrator, ChangelogOrchestrator>();
    services.AddScoped<IBranchKeyExtractor, BranchKeyExtractor>();
    services.AddScoped<IEpicResolver, EpicResolver>();
    
    // Caching
    services.AddMemoryCache();
    
    // Validation
    services.AddScoped<IValidator<BuildReportRequest>, BuildReportRequestValidator>();
}
```

---

## 6. Error Handling & Validation

### 6.1 Request Validation
```csharp
public class BuildReportRequestValidator : AbstractValidator<BuildReportRequest>
{
    public BuildReportRequestValidator()
    {
        RuleFor(x => x.Repos)
            .NotEmpty()
            .WithMessage("At least one repository must be selected")
            .Must(repos => repos.Count <= 10)
            .WithMessage("Maximum 10 repositories allowed");
            
        RuleFor(x => x.ReleaseBranch)
            .NotEmpty()
            .Matches(@"^release/\d{4}-\d{2}$")
            .WithMessage("Release branch must match format 'release/YYYY-MM'");
            
        RuleFor(x => x.From)
            .LessThan(x => x.To)
            .WithMessage("From date must be before To date");
            
        RuleFor(x => x)
            .Must(x => (x.To - x.From).TotalDays <= 90)
            .WithMessage("Date range cannot exceed 90 days");
    }
}
```

### 6.2 Global Exception Handling
```csharp
public class GlobalExceptionMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            await HandleValidationException(context, ex);
        }
        catch (HttpRequestException ex)
        {
            await HandleHttpException(context, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleGenericException(context, ex);
        }
    }
    
    private async Task HandleValidationException(HttpContext context, ValidationException ex)
    {
        var problemDetails = new ValidationProblemDetails(
            ex.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
        {
            Status = 400,
            Title = "Bad Request",
            Detail = "One or more validation errors occurred."
        };
        
        context.Response.StatusCode = 400;
        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
```

---

## 7. Performance & Caching

### 7.1 Caching Strategy
```csharp
public class JiraService : IJiraService
{
    private readonly IMemoryCache _cache;
    
    public async Task<Dictionary<string, JiraIssue>> GetIssuesBatchAsync(
        IEnumerable<string> issueKeys,
        CancellationToken cancellationToken = default)
    {
        var keys = issueKeys.ToList();
        var result = new Dictionary<string, JiraIssue>();
        var uncachedKeys = new List<string>();
        
        // Check cache first
        foreach (var key in keys)
        {
            if (_cache.TryGetValue($"jira:issue:{key}", out JiraIssue cachedIssue))
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
            var freshIssues = await FetchIssuesFromApi(uncachedKeys, cancellationToken);
            
            foreach (var (key, issue) in freshIssues)
            {
                _cache.Set($"jira:issue:{key}", issue, TimeSpan.FromMinutes(15));
                result[key] = issue;
            }
        }
        
        return result;
    }
}
```

### 7.2 Parallel Processing
```csharp
public async Task<BuildReportResponse> GenerateReportAsync(
    BuildReportRequest request,
    CancellationToken cancellationToken = default)
{
    // Process all repos in parallel
    var repoTasks = request.Repos.Select(async repo =>
    {
        var prs = await _bitbucketService.GetMergedPullRequestsAsync(
            repo, request.ReleaseBranch, request.From, request.To, cancellationToken);
        return (Repo: repo, PullRequests: prs);
    });
    
    var repoResults = await Task.WhenAll(repoTasks);
    
    // Collect all Jira keys from all repos
    var allJiraKeys = repoResults
        .SelectMany(r => r.PullRequests)
        .SelectMany(pr => _branchKeyExtractor.ExtractJiraKeys(pr.SourceBranch))
        .Distinct()
        .ToList();
    
    // Fetch all Jira issues in one batch
    var jiraIssues = await _jiraService.GetIssuesBatchAsync(allJiraKeys, cancellationToken);
    
    // Build stories
    var storyTasks = repoResults.Select(async r =>
        await BuildStoriesForRepo(r.Repo, r.PullRequests, jiraIssues, cancellationToken));
    
    var storyResults = await Task.WhenAll(storyTasks);
    var allStories = storyResults.SelectMany(s => s).ToList();
    
    return new BuildReportResponse(/* ... */);
}
```

---

## 8. Logging & Observability

### 8.1 Structured Logging
```csharp
public class ChangelogOrchestrator : IChangelogOrchestrator
{
    private readonly ILogger<ChangelogOrchestrator> _logger;
    
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
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await GenerateReportInternalAsync(request, cancellationToken);
            
            _logger.LogInformation("Changelog generation completed in {ElapsedMs}ms. Found {StoryCount} stories",
                stopwatch.ElapsedMilliseconds, result.Stories.Count);
                
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Changelog generation failed after {ElapsedMs}ms", 
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

### 8.2 Performance Metrics
```csharp
public class MetricsService
{
    private static readonly Counter ReportGenerations = Metrics
        .CreateCounter("changelog_reports_total", "Total changelog reports generated");
        
    private static readonly Histogram ReportDuration = Metrics
        .CreateHistogram("changelog_generation_duration_seconds", "Time to generate changelog");
        
    private static readonly Counter BitbucketApiCalls = Metrics
        .CreateCounter("bitbucket_api_calls_total", "Total Bitbucket API calls", new[] { "endpoint", "status" });
}
```

---

## 9. Deployment & Infrastructure

### 9.1 Docker Configuration
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Changelog.Api.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Changelog.Api.dll"]
```

### 9.2 Health Checks
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddHealthChecks()
        .AddCheck<BitbucketHealthCheck>("bitbucket")
        .AddCheck<JiraHealthCheck>("jira")
        .AddCheck("memory", () =>
        {
            var gc = GC.GetTotalMemory(false);
            return gc < 500_000_000 ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy();
        });
}

public class BitbucketHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/rest/api/1.0/application-properties", cancellationToken);
            return response.IsSuccessStatusCode ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(ex.Message);
        }
    }
}
```

---

## 10. Security Considerations

### 10.1 Authentication & Authorization
- Service account credentials stored in Azure KeyVault or environment variables
- API requires no authentication (internal tool)
- HTTPS enforced in production
- CORS configured for specific frontend domains

### 10.2 Input Sanitization
- All user inputs validated using FluentValidation
- SQL injection not applicable (no direct DB access)
- Branch name parsing uses regex with strict patterns
- Date range limits prevent resource exhaustion

### 10.3 Secrets Management
```csharp
public void ConfigureServices(IServiceCollection services)
{
    if (Environment.IsProduction())
    {
        var keyVaultUrl = Configuration["KeyVault:Url"];
        Configuration.AddAzureKeyVault(keyVaultUrl, new DefaultAzureCredential());
    }
    
    services.Configure<BitbucketConfig>(config =>
    {
        config.AccessToken = Configuration["BitbucketAccessToken"] ?? config.AccessToken;
    });
    
    services.Configure<JiraConfig>(config =>
    {
        config.AccessToken = Configuration["JiraAccessToken"] ?? config.AccessToken;
    });
}
```

---

This backend architecture provides a robust, scalable foundation for the SIT Changelog Builder with clear separation of concerns, comprehensive error handling, and production-ready features like caching, logging, and health checks.