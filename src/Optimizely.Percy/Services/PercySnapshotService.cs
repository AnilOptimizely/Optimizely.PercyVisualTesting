using System.Diagnostics;
using System.Text;
using EPiServer.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Optimizely.Percy.Configuration;
using Optimizely.Percy.Models;

namespace Optimizely.Percy.Services;

/// <summary>
/// Triggers Percy snapshots via the Percy CLI (npx percy snapshot) and retrieves build data
/// from the Percy REST API.
/// </summary>
public class PercySnapshotService : IPercySnapshotService
{
    private readonly PercyOptions _options;
    private readonly IPageUrlResolver _urlResolver;
    private readonly IPercyApiClient _apiClient;
    private readonly ILogger<PercySnapshotService> _logger;

    private static readonly TimeSpan CliTimeout = TimeSpan.FromMinutes(5);

    public PercySnapshotService(
        IOptions<PercyOptions> options,
        IPageUrlResolver urlResolver,
        IPercyApiClient apiClient,
        ILogger<PercySnapshotService> logger)
    {
        _options = options.Value;
        _urlResolver = urlResolver;
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PercyBuildResult> TriggerSnapshotAsync(IContent content, string? label = null)
    {
        var relativeUrl = _urlResolver.ResolveUrl(content);
        if (string.IsNullOrWhiteSpace(relativeUrl))
        {
            _logger.LogWarning("Could not resolve URL for content '{Name}' ({Id}). Snapshot skipped.",
                content.Name, content.ContentLink?.ID);
            return new PercyBuildResult(false, "", "", $"Could not resolve URL for '{content.Name}'.", 0);
        }

        var snapshotLabel = label ?? content.Name;
        return await TriggerBatchSnapshotAsync([relativeUrl], snapshotLabel);
    }

    /// <inheritdoc />
    public async Task<PercyBuildResult> TriggerBatchSnapshotAsync(IEnumerable<string> relativeUrls, string buildLabel)
    {
        if (string.IsNullOrWhiteSpace(_options.Token))
        {
            _logger.LogWarning("Percy token is not configured. Snapshot skipped.");
            return new PercyBuildResult(false, "", "", "Percy token is not configured.", 0);
        }

        var urls = relativeUrls?.ToList() ?? [];
        if (urls.Count == 0)
        {
            return new PercyBuildResult(false, "", "", "No URLs provided for snapshot.", 0);
        }

        var yamlFile = Path.GetTempFileName();
        try
        {
            var yaml = BuildSnapshotYaml(urls, buildLabel);
            await File.WriteAllTextAsync(yamlFile, yaml, Encoding.UTF8);

            _logger.LogInformation("Triggering Percy snapshot for {Count} URL(s) with label '{Label}'", urls.Count, buildLabel);

            var (exitCode, stdout, stderr) = await RunPercyCliAsync(yamlFile);

            if (exitCode != 0)
            {
                _logger.LogError("Percy CLI exited with code {Code}. Stderr: {Err}", exitCode, stderr);
                return new PercyBuildResult(false, "", "", $"Percy CLI failed (exit {exitCode}): {stderr}", 0);
            }

            _logger.LogInformation("Percy CLI completed successfully. Output: {Out}", stdout);
            return new PercyBuildResult(true, "", "", "Snapshot triggered successfully.", urls.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while triggering Percy snapshot");
            return new PercyBuildResult(false, "", "", ex.Message, 0);
        }
        finally
        {
            if (File.Exists(yamlFile))
                File.Delete(yamlFile);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PercyBuildSummary>> GetRecentBuildsAsync(int count = 10)
        => _apiClient.GetBuildsAsync(_options.ProjectSlug, count);

    /// <inheritdoc />
    public Task<string> GetBuildDiffUrlAsync(string buildId)
        => _apiClient.GetBuildWebUrlAsync(buildId);

    private static string EscapeYamlString(string value)
    {
        // Escape backslashes and double quotes within the YAML double-quoted scalar
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private string BuildSnapshotYaml(IEnumerable<string> relativeUrls, string buildLabel)
    {
        var baseUrl = _options.BaseUrl.TrimEnd('/');
        var sb = new StringBuilder();
        sb.AppendLine($"# Percy snapshot configuration — {buildLabel}");
        sb.AppendLine("snapshots:");

        foreach (var relUrl in relativeUrls)
        {
            var absoluteUrl = string.IsNullOrWhiteSpace(baseUrl) ? relUrl : $"{baseUrl}{relUrl}";
            var safeName = EscapeYamlString(buildLabel);
            var safeUrl = EscapeYamlString(absoluteUrl);
            sb.AppendLine($"  - name: \"{safeName}\"");
            sb.AppendLine($"    url: \"{safeUrl}\"");

            if (_options.SnapshotWidths?.Length > 0)
            {
                sb.AppendLine($"    widths: [{string.Join(", ", _options.SnapshotWidths)}]");
            }

            if (_options.MinHeight > 0)
            {
                sb.AppendLine($"    minHeight: {_options.MinHeight}");
            }

            if (!string.IsNullOrWhiteSpace(_options.PercyCss))
            {
                sb.AppendLine($"    percyCSS: |");
                foreach (var line in _options.PercyCss.Split('\n'))
                    sb.AppendLine($"      {line}");
            }
        }

        return sb.ToString();
    }

    private async Task<(int exitCode, string stdout, string stderr)> RunPercyCliAsync(string yamlFile)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _options.PercyCliPath,
            Arguments = $"percy snapshot \"{yamlFile}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        psi.Environment["PERCY_TOKEN"] = _options.Token;

        using var process = new Process { StartInfo = psi };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        var completedInTime = await Task.WhenAny(
            process.WaitForExitAsync(),
            Task.Delay(CliTimeout));

        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            return (-1, "", "Percy CLI timed out after 5 minutes.");
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        return (process.ExitCode, stdout, stderr);
    }
}
