namespace Optimizely.Percy.Models;

/// <summary>Result of a Percy snapshot build trigger operation.</summary>
public record PercyBuildResult(bool Success, string BuildId, string BuildUrl, string Message, int SnapshotCount);
