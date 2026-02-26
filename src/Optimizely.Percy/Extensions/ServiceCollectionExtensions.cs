using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Optimizely.Percy.Configuration;
using Optimizely.Percy.Services;

namespace Optimizely.Percy.Extensions;

/// <summary>Extension methods for registering Percy services.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Percy visual testing services and configures options from the
    /// <c>Optimizely:Percy</c> configuration section.
    /// </summary>
    public static IServiceCollection AddPercyVisualTesting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PercyOptions>(configuration.GetSection("Optimizely:Percy"));

        services.AddSingleton<IPageUrlResolver, PageUrlResolver>();

        services.AddHttpClient<IPercyApiClient, PercyApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://percy.io/api/v1/");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        services.AddSingleton<IPercySnapshotService, PercySnapshotService>();

        return services;
    }
}
