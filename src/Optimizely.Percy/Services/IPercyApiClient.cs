using Optimizely.Percy.Models;

namespace Optimizely.Percy.Services;

/// <summary>Client for the Percy REST API.</summary>
public interface IPercyApiClient
{
    /// <summary>Returns recent builds for the given project slug.</summary>
    Task<IReadOnlyList<PercyBuildSummary>> GetBuildsAsync(string projectSlug, int limit = 10);

    /// <summary>Returns details for a specific build.</summary>
    Task<PercyBuildSummary?> GetBuildAsync(string buildId);

    /// <summary>Returns all snapshots for a given build.</summary>
    Task<IReadOnlyList<PercySnapshotInfo>> GetSnapshotsForBuildAsync(string buildId);

    /// <summary>Returns the web URL for a build's diff view.</summary>
    Task<string> GetBuildWebUrlAsync(string buildId);
}
