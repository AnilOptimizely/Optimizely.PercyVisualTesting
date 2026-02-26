using Xunit;
using Moq;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Optimizely.Percy.Services;
using Optimizely.Percy.Configuration;
using Optimizely.Percy.Models;
using EPiServer.Core;

namespace Optimizely.Percy.Tests.Services;

public class PercySnapshotServiceTests
{
    private readonly Mock<IPageUrlResolver> _urlResolverMock = new();
    private readonly Mock<IPercyApiClient> _apiClientMock = new();
    private readonly Mock<ILogger<PercySnapshotService>> _loggerMock = new();

    private PercySnapshotService CreateService(PercyOptions? options = null)
    {
        var opts = Options.Create(options ?? new PercyOptions { Token = "test-token", BaseUrl = "https://test.com" });
        return new PercySnapshotService(opts, _urlResolverMock.Object, _apiClientMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task TriggerSnapshotAsync_WhenUrlCannotBeResolved_ReturnsFailure()
    {
        var service = CreateService();
        var contentMock = new Mock<IContent>();
        contentMock.Setup(c => c.ContentLink).Returns(new ContentReference(1));
        contentMock.Setup(c => c.Name).Returns("Test Page");
        _urlResolverMock.Setup(r => r.ResolveUrl(It.IsAny<IContent>())).Returns((string?)null);

        var result = await service.TriggerSnapshotAsync(contentMock.Object);

        Assert.False(result.Success);
        Assert.Contains("resolve URL", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TriggerBatchSnapshotAsync_WhenTokenIsEmpty_ReturnsFailure()
    {
        var service = CreateService(new PercyOptions { Token = "", BaseUrl = "https://test.com" });

        var result = await service.TriggerBatchSnapshotAsync(["/en/home"], "test-label");

        Assert.False(result.Success);
        Assert.Contains("token", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TriggerBatchSnapshotAsync_WhenNoUrls_ReturnsFailure()
    {
        var service = CreateService();

        var result = await service.TriggerBatchSnapshotAsync([], "test-label");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GetRecentBuildsAsync_DelegatesToApiClient()
    {
        var builds = new List<PercyBuildSummary>
        {
            new("id1", "https://percy.io/builds/1", "finished", 5, 5, 0, DateTimeOffset.UtcNow, null)
        };
        _apiClientMock
            .Setup(c => c.GetBuildsAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(builds);

        var service = CreateService(new PercyOptions
        {
            Token = "test",
            BaseUrl = "https://test.com",
            ProjectSlug = "org/proj"
        });

        var result = await service.GetRecentBuildsAsync(10);

        Assert.Equal(builds, result);
        _apiClientMock.Verify(c => c.GetBuildsAsync("org/proj", 10), Times.Once);
    }

    [Fact]
    public async Task GetBuildDiffUrlAsync_ReturnsUrlFromApiClient()
    {
        _apiClientMock
            .Setup(c => c.GetBuildWebUrlAsync("build123"))
            .ReturnsAsync("https://percy.io/builds/123");

        var service = CreateService();

        var url = await service.GetBuildDiffUrlAsync("build123");

        Assert.Equal("https://percy.io/builds/123", url);
    }

    [Fact]
    public async Task GetRecentBuildsAsync_WhenNoProjectSlug_ReturnsEmptyFromApi()
    {
        _apiClientMock
            .Setup(c => c.GetBuildsAsync(string.Empty, It.IsAny<int>()))
            .ReturnsAsync(new List<PercyBuildSummary>());

        var service = CreateService(new PercyOptions { Token = "test", ProjectSlug = "" });

        var result = await service.GetRecentBuildsAsync();

        Assert.Empty(result);
    }
}
