namespace Optimizely.Percy.Models;

/// <summary>Information about a single snapshot within a Percy build.</summary>
public record PercySnapshotInfo(string SnapshotId, string Name, string ReviewState, string? DiffUrl);
