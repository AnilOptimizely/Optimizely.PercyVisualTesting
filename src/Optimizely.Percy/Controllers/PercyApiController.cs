using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Optimizely.Percy.Models;
using Optimizely.Percy.Services;

namespace Optimizely.Percy.Controllers;

/// <summary>REST API controller that exposes Percy build and snapshot data to the editor UI.</summary>
[ApiController]
[Authorize(Policy = "episerver:cmseditor")]
[Route("api/percy")]
public class PercyApiController : ControllerBase
{
    private readonly IPercySnapshotService _snapshotService;

    public PercyApiController(IPercySnapshotService snapshotService)
    {
        _snapshotService = snapshotService;
    }

    /// <summary>Returns recent Percy builds.</summary>
    [HttpGet("builds")]
    public async Task<ActionResult<IReadOnlyList<PercyBuildSummary>>> GetBuilds([FromQuery] int count = 10)
    {
        var builds = await _snapshotService.GetRecentBuildsAsync(count);
        return Ok(builds);
    }

    /// <summary>Returns the diff URL for the given build.</summary>
    [HttpGet("builds/{buildId}/diff")]
    public async Task<ActionResult<string>> GetBuildDiff(string buildId)
    {
        if (string.IsNullOrWhiteSpace(buildId))
            return BadRequest("buildId is required.");

        var url = await _snapshotService.GetBuildDiffUrlAsync(buildId);
        return Ok(new { diffUrl = url });
    }

    /// <summary>Triggers a Percy snapshot for the given URLs.</summary>
    /// <remarks>Expects the antiforgery token in the <c>RequestVerificationToken</c> request header.</remarks>
    [HttpPost("snapshot")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<PercyBuildResult>> TriggerSnapshot([FromBody] ManualSnapshotRequest request)
    {
        if (request?.Urls == null || request.Urls.Length == 0)
            return BadRequest(new { error = "At least one URL is required." });

        var label = request.Label ?? $"API trigger {DateTime.UtcNow:yyyy-MM-dd HH:mm}";
        var result = await _snapshotService.TriggerBatchSnapshotAsync(request.Urls, label);
        return result.Success ? Ok(result) : StatusCode(500, result);
    }
}
