using EPiServer.Core;
using Optimizely.Percy.Models;

namespace Optimizely.Percy.Services;

/// <summary>Service for triggering Percy visual testing snapshots and retrieving build information.</summary>
public interface IPercySnapshotService
{
    /// <summary>Triggers a Percy snapshot for a single content item.</summary>
    Task<PercyBuildResult> TriggerSnapshotAsync(IContent content, string? label = null);

    /// <summary>Triggers Percy snapshots for a batch of relative URLs.</summary>
    Task<PercyBuildResult> TriggerBatchSnapshotAsync(IEnumerable<string> relativeUrls, string buildLabel);

    /// <summary>Returns the most recent Percy builds.</summary>
    Task<IReadOnlyList<PercyBuildSummary>> GetRecentBuildsAsync(int count = 10);

    /// <summary>Returns the web diff URL for the given build.</summary>
    Task<string> GetBuildDiffUrlAsync(string buildId);
}
