using Microsoft.AspNetCore.Mvc;
using BofA.MBSS.ChnageLog.Models;
using BofA.MBSS.ChnageLog.Services;

namespace BofA.MBSS.ChnageLog.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportController : ControllerBase
{
    private readonly IChangelogOrchestrator _orchestrator;
    private readonly ILogger<ReportController> _logger;

    public ReportController(IChangelogOrchestrator orchestrator, ILogger<ReportController> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    /// <summary>
    /// Generate changelog report for specified repositories and date range
    /// </summary>
    /// <param name="request">Report generation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Changelog report with stories and summaries</returns>
    [HttpPost("build")]
    [ProducesResponseType(typeof(BuildReportResponse), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<BuildReportResponse>> BuildReport(
        [FromBody] BuildReportRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Received changelog build request for {ReleaseBranch} with {RepoCount} repos", 
            request.ReleaseBranch, request.Repos.Count);

        try
        {
            // Validate model
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _orchestrator.GenerateReportAsync(request, cancellationToken);
            
            _logger.LogInformation("Successfully generated changelog with {StoryCount} stories", response.Stories.Count);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request parameters");
            ModelState.AddModelError("", ex.Message);
            return BadRequest(ModelState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating changelog report");
            return StatusCode(500, "An error occurred while generating the changelog report");
        }
    }
}
