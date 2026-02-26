namespace Optimizely.Percy.Models;

/// <summary>Request body for manually triggering Percy snapshots for a set of URLs.</summary>
public record ManualSnapshotRequest(string[] Urls, string? Label);
