using EPiServer.Core;
using EPiServer.Web.Routing;
using Microsoft.Extensions.Logging;

namespace Optimizely.Percy.Services;

/// <summary>Uses the EPiServer URL resolver to obtain relative URLs for content items.</summary>
public class PageUrlResolver : IPageUrlResolver
{
    private readonly IUrlResolver _urlResolver;
    private readonly ILogger<PageUrlResolver> _logger;

    public PageUrlResolver(IUrlResolver urlResolver, ILogger<PageUrlResolver> logger)
    {
        _urlResolver = urlResolver;
        _logger = logger;
    }

    /// <inheritdoc />
    public string? ResolveUrl(IContent content)
    {
        if (content == null) return null;

        try
        {
            var url = _urlResolver.GetUrl(content);
            if (string.IsNullOrWhiteSpace(url))
            {
                _logger.LogDebug("No URL resolved for content '{Name}' ({Id})", content.Name, content.ContentLink?.ID);
                return null;
            }

            return url;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve URL for content '{Name}' ({Id})", content.Name, content.ContentLink?.ID);
            return null;
        }
    }
}
