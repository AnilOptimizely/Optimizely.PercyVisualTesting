using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Optimizely.Percy.Configuration;
using Optimizely.Percy.Models;

namespace Optimizely.Percy.Services;

/// <summary>HTTP client wrapper for the Percy REST API v1.</summary>
public class PercyApiClient : IPercyApiClient
{
    private readonly HttpClient _httpClient;
    private readonly PercyOptions _options;
    private readonly ILogger<PercyApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public PercyApiClient(HttpClient httpClient, IOptions<PercyOptions> options, ILogger<PercyApiClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    private void ApplyAuthHeader()
    {
        if (!string.IsNullOrWhiteSpace(_options.Token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Token", $"token={_options.Token}");
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PercyBuildSummary>> GetBuildsAsync(string projectSlug, int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(projectSlug)) return [];

        try
        {
            ApplyAuthHeader();
            var response = await _httpClient.GetAsync($"projects/{Uri.EscapeDataString(projectSlug)}/builds?page[limit]={limit}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Percy API returned {Status} for GetBuilds", response.StatusCode);
                return [];
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
                return [];

            var builds = new List<PercyBuildSummary>();
            foreach (var item in data.EnumerateArray())
            {
                var summary = ParseBuildSummary(item);
                if (summary != null) builds.Add(summary);
            }

            return builds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Percy builds for project '{Slug}'", projectSlug);
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<PercyBuildSummary?> GetBuildAsync(string buildId)
    {
        if (string.IsNullOrWhiteSpace(buildId)) return null;

        try
        {
            ApplyAuthHeader();
            var response = await _httpClient.GetAsync($"builds/{Uri.EscapeDataString(buildId)}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Percy API returned {Status} for GetBuild({Id})", response.StatusCode, buildId);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("data", out var data))
                return null;

            return ParseBuildSummary(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Percy build '{BuildId}'", buildId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PercySnapshotInfo>> GetSnapshotsForBuildAsync(string buildId)
    {
        if (string.IsNullOrWhiteSpace(buildId)) return [];

        try
        {
            ApplyAuthHeader();
            var response = await _httpClient.GetAsync($"builds/{Uri.EscapeDataString(buildId)}/snapshots");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Percy API returned {Status} for GetSnapshots({Id})", response.StatusCode, buildId);
                return [];
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
                return [];

            var snapshots = new List<PercySnapshotInfo>();
            foreach (var item in data.EnumerateArray())
            {
                var snap = ParseSnapshotInfo(item);
                if (snap != null) snapshots.Add(snap);
            }

            return snapshots;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching snapshots for build '{BuildId}'", buildId);
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<string> GetBuildWebUrlAsync(string buildId)
    {
        var build = await GetBuildAsync(buildId);
        return build?.BuildUrl ?? string.Empty;
    }

    private static PercyBuildSummary? ParseBuildSummary(JsonElement element)
    {
        try
        {
            var id = element.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";
            var attrs = element.TryGetProperty("attributes", out var a) ? a : element;

            var buildUrl = attrs.TryGetProperty("web-url", out var wu) ? wu.GetString() ?? "" : "";
            var state = attrs.TryGetProperty("state", out var st) ? st.GetString() ?? "" : "";
            var totalSnapshots = attrs.TryGetProperty("total-snapshots", out var ts) ? ts.GetInt32() : 0;
            var totalComparisons = attrs.TryGetProperty("total-comparisons", out var tc) ? tc.GetInt32() : 0;
            var unreviewedComparisons = attrs.TryGetProperty("total-comparisons-unreviewed", out var uc) ? uc.GetInt32() : 0;
            var createdAt = attrs.TryGetProperty("created-at", out var ca) && ca.TryGetDateTimeOffset(out var dt) ? dt : DateTimeOffset.UtcNow;
            var label = attrs.TryGetProperty("build-label", out var bl) ? bl.GetString() : null;

            return new PercyBuildSummary(id, buildUrl, state, totalSnapshots, totalComparisons, unreviewedComparisons, createdAt, label);
        }
        catch
        {
            return null;
        }
    }

    private static PercySnapshotInfo? ParseSnapshotInfo(JsonElement element)
    {
        try
        {
            var id = element.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";
            var attrs = element.TryGetProperty("attributes", out var a) ? a : element;

            var name = attrs.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
            var reviewState = attrs.TryGetProperty("review-state", out var rs) ? rs.GetString() ?? "" : "";
            var diffUrl = attrs.TryGetProperty("diff-url", out var du) ? du.GetString() : null;

            return new PercySnapshotInfo(id, name, reviewState, diffUrl);
        }
        catch
        {
            return null;
        }
    }
}
