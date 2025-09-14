using Microsoft.AspNetCore.Mvc;
using BofA.MBSS.ChnageLog.Models;

namespace BofA.MBSS.ChnageLog.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MetaController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MetaController> _logger;

    public MetaController(IConfiguration configuration, ILogger<MetaController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Get list of available repositories and default release branch
    /// </summary>
    /// <returns>Repository list with default settings</returns>
    [HttpGet("repos")]
    [ProducesResponseType(typeof(RepoListResponse), 200)]
    public ActionResult<RepoListResponse> GetRepositories()
    {
        try
        {
            var repoSection = _configuration.GetSection("Repositories");
            var repos = repoSection.Get<List<RepoRef>>() ?? new List<RepoRef>();
            
            var defaultBranch = _configuration["Validation:DefaultReleaseBranch"] ?? "release/2025-09";

            var response = new RepoListResponse(repos, defaultBranch);
            
            _logger.LogDebug("Returning {RepoCount} repositories with default branch {DefaultBranch}", 
                repos.Count, defaultBranch);
                
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving repository list");
            return StatusCode(500, "An error occurred while retrieving repository information");
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    /// <returns>Application health status</returns>
    [HttpGet("health")]
    [ProducesResponseType(typeof(HealthResponse), 200)]
    public ActionResult<HealthResponse> Health()
    {
        var response = new HealthResponse(
            "Healthy",
            DateTimeOffset.UtcNow,
            "1.0.0");

        return Ok(response);
    }
}
