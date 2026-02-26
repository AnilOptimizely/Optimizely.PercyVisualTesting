namespace Optimizely.Percy.Configuration;

/// <summary>Configuration options for Percy visual testing integration.</summary>
public class PercyOptions
{
    /// <summary>Percy API token (PERCY_TOKEN).</summary>
    public string Token { get; set; } = "";

    /// <summary>Base URL of the CMS site used to generate absolute snapshot URLs.</summary>
    public string BaseUrl { get; set; } = "";

    /// <summary>Viewport widths for snapshots in pixels.</summary>
    public int[] SnapshotWidths { get; set; } = [375, 768, 1280];

    /// <summary>Minimum height for snapshots in pixels.</summary>
    public int MinHeight { get; set; } = 1024;

    /// <summary>When true, Percy snapshots are triggered automatically on content publish.</summary>
    public bool TriggerOnPublish { get; set; } = true;

    /// <summary>CSS selectors for elements to hide in snapshots.</summary>
    public string[] IgnoreSelectors { get; set; } = [];

    /// <summary>Custom Percy CSS injected into each snapshot.</summary>
    public string PercyCss { get; set; } = "";

    /// <summary>Only snapshot content of these content type names (empty = all types).</summary>
    public string[] IncludeContentTypes { get; set; } = [];

    /// <summary>Exclude content of these content type names from snapshots.</summary>
    public string[] ExcludeContentTypes { get; set; } = [];

    /// <summary>When true, the Percy dashboard admin view is enabled.</summary>
    public bool EnableDashboard { get; set; } = true;

    /// <summary>Percy project slug in org/project format used for API calls.</summary>
    public string ProjectSlug { get; set; } = "";

    /// <summary>Path or command used to invoke Percy CLI (default: npx).</summary>
    public string PercyCliPath { get; set; } = "npx";

    /// <summary>Validates configuration and returns a list of error messages.</summary>
    public List<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Token))
            errors.Add("Percy Token is required.");
        if (!string.IsNullOrWhiteSpace(BaseUrl) && !Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
            errors.Add("BaseUrl must be a valid absolute URL.");
        return errors;
    }
}
