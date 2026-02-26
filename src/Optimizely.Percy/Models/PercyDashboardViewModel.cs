using Optimizely.Percy.Configuration;

namespace Optimizely.Percy.Models;

/// <summary>View model for the Percy admin dashboard page.</summary>
public class PercyDashboardViewModel
{
    /// <summary>Current Percy configuration options.</summary>
    public PercyOptions Options { get; set; } = new();

    /// <summary>Recent builds retrieved from the Percy API.</summary>
    public IReadOnlyList<PercyBuildSummary> RecentBuilds { get; set; } = [];

    /// <summary>Configuration validation errors, if any.</summary>
    public List<string> ValidationErrors { get; set; } = [];

    /// <summary>Returns true when a Percy token is configured.</summary>
    public bool IsConfigured => !string.IsNullOrEmpty(Options?.Token);
}
