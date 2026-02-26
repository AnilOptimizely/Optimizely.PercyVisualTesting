using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Optimizely.Percy.Configuration;
using Optimizely.Percy.Models;
using Optimizely.Percy.Services;

namespace Optimizely.Percy.Controllers;

/// <summary>Admin controller that renders the Percy dashboard and exposes management actions.</summary>
[Authorize(Policy = "episerver:cmsadmin")]
[Route("percy-admin")]
public class PercyAdminController : Controller
{
    private readonly IPercySnapshotService _snapshotService;
    private readonly IOptions<PercyOptions> _options;

    public PercyAdminController(IPercySnapshotService snapshotService, IOptions<PercyOptions> options)
    {
        _snapshotService = snapshotService;
        _options = options;
    }

    /// <summary>Renders the Percy admin dashboard.</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var opts = _options.Value;
        var vm = new PercyDashboardViewModel
        {
            Options = opts,
            ValidationErrors = opts.Validate()
        };

        if (vm.IsConfigured)
        {
            vm.RecentBuilds = await _snapshotService.GetRecentBuildsAsync(10);
        }

        return View(vm);
    }

    /// <summary>Returns the recent builds as JSON.</summary>
    [HttpGet("builds")]
    public async Task<IActionResult> Builds([FromQuery] int count = 10)
    {
        var builds = await _snapshotService.GetRecentBuildsAsync(count);
        return Json(builds);
    }

    /// <summary>Triggers a batch Percy snapshot for the supplied URLs.</summary>
    /// <remarks>
    /// Expects the antiforgery token in the <c>RequestVerificationToken</c> request header
    /// (ASP.NET Core default; configurable via <c>AntiforgeryOptions.HeaderName</c>).
    /// </remarks>
    [HttpPost("trigger")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Trigger([FromBody] ManualSnapshotRequest request)
    {
        if (request?.Urls == null || request.Urls.Length == 0)
            return BadRequest(new { error = "At least one URL is required." });

        var label = request.Label ?? $"Manual trigger {DateTime.UtcNow:yyyy-MM-dd HH:mm}";
        var result = await _snapshotService.TriggerBatchSnapshotAsync(request.Urls, label);
        return Json(result);
    }

    /// <summary>Returns Percy configuration health status.</summary>
    [HttpGet("status")]
    public IActionResult Status()
    {
        var opts = _options.Value;
        var errors = opts.Validate();
        return Json(new
        {
            configured = string.IsNullOrWhiteSpace(opts.Token) == false,
            valid = errors.Count == 0,
            errors,
            projectSlug = opts.ProjectSlug,
            triggerOnPublish = opts.TriggerOnPublish,
            dashboardEnabled = opts.EnableDashboard
        });
    }
}
