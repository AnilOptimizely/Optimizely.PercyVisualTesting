using EPiServer.Core;

namespace Optimizely.Percy.Services;

/// <summary>Resolves a routable URL for a given CMS content item.</summary>
public interface IPageUrlResolver
{
    /// <summary>
    /// Returns the relative URL for <paramref name="content"/>,
    /// or <c>null</c> when the content is not routable.
    /// </summary>
    string? ResolveUrl(IContent content);
}
