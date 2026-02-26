using EPiServer.Core;
using EPiServer.Framework;
using EPiServer.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Optimizely.Percy.Configuration;
using Optimizely.Percy.Services;

namespace Optimizely.Percy.EventHandlers;

/// <summary>
/// Initializable module that hooks into the CMS publish event and triggers Percy visual snapshots
/// when content is published.
/// </summary>
[InitializableModule]
[ModuleDependency(typeof(InitializationModule))]
public class ContentPublishPercyHandler : IInitializableModule
{
    private IServiceProvider? _serviceProvider;

    /// <inheritdoc />
    public void Initialize(InitializationEngine context)
    {
        _serviceProvider = context.ServiceLocator;
        var contentEvents = _serviceProvider?.GetService<IContentEvents>();
        if (contentEvents != null)
        {
            contentEvents.PublishedContent += OnPublishedContent;
        }
    }

    /// <inheritdoc />
    public void Uninitialize(InitializationEngine context)
    {
        var contentEvents = _serviceProvider?.GetService<IContentEvents>();
        if (contentEvents != null)
        {
            contentEvents.PublishedContent -= OnPublishedContent;
        }
    }

    private void OnPublishedContent(object? sender, ContentEventArgs e)
    {
        if (e.Content == null || _serviceProvider == null) return;

        var options = _serviceProvider.GetService<IOptions<PercyOptions>>()?.Value;
        if (options == null || !options.TriggerOnPublish || string.IsNullOrWhiteSpace(options.Token))
            return;

        if (!ShouldSnapshot(e.Content, options))
            return;

        var content = e.Content;
        // Fire-and-forget: we don't want to block the publish pipeline
        _ = Task.Run(async () =>
        {
            var logger = _serviceProvider.GetService<ILogger<ContentPublishPercyHandler>>();
            var snapshotService = _serviceProvider.GetService<IPercySnapshotService>();
            if (snapshotService == null) return;

            try
            {
                var result = await snapshotService.TriggerSnapshotAsync(content, $"publish:{content.Name}");
                if (result.Success)
                    logger?.LogInformation("Percy snapshot triggered for '{Name}' on publish.", content.Name);
                else
                    logger?.LogWarning("Percy snapshot failed for '{Name}': {Msg}", content.Name, result.Message);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Unhandled error triggering Percy snapshot for '{Name}'", content.Name);
            }
        });
    }

    private static bool ShouldSnapshot(IContent content, PercyOptions options)
    {
        var typeName = content.GetType().Name;

        if (options.IncludeContentTypes.Length > 0 &&
            !options.IncludeContentTypes.Contains(typeName, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (options.ExcludeContentTypes.Contains(typeName, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }
}
