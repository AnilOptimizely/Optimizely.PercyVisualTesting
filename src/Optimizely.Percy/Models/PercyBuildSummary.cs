namespace Optimizely.Percy.Models;

/// <summary>Summary information about a Percy build.</summary>
public record PercyBuildSummary(
    string BuildId,
    string BuildUrl,
    string State,
    int TotalSnapshots,
    int TotalComparisons,
    int UnreviewedComparisons,
    DateTimeOffset CreatedAt,
    string? TriggerLabel);
