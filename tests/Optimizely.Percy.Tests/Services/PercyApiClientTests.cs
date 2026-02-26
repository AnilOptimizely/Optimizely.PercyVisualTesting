using System.Net;
using System.Text;
using Xunit;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Moq;
using Optimizely.Percy.Services;
using Optimizely.Percy.Configuration;

namespace Optimizely.Percy.Tests.Services;

/// <summary>Fake HTTP handler that returns a preset response.</summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _response;

    public FakeHttpMessageHandler(HttpStatusCode statusCode, string json)
    {
        _response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(_response);
}

public class PercyApiClientTests
{
    private PercyApiClient CreateClient(HttpMessageHandler handler, PercyOptions? options = null)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://percy.io/api/v1/")
        };
        var opts = Options.Create(options ?? new PercyOptions { Token = "test-token" });
        var logger = Mock.Of<ILogger<PercyApiClient>>();
        return new PercyApiClient(httpClient, opts, logger);
    }

    [Fact]
    public async Task GetBuildsAsync_WithEmptySlug_ReturnsEmpty()
    {
        var client = CreateClient(new FakeHttpMessageHandler(HttpStatusCode.OK, "{}"));

        var result = await client.GetBuildsAsync("");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetBuildsAsync_WhenServerReturnsError_ReturnsEmpty()
    {
        var client = CreateClient(new FakeHttpMessageHandler(HttpStatusCode.Unauthorized, "{}"));

        var result = await client.GetBuildsAsync("org/proj");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetBuildsAsync_ParsesBuildsArray()
    {
        const string json = """
        {
          "data": [
            {
              "id": "build-1",
              "attributes": {
                "web-url": "https://percy.io/builds/build-1",
                "state": "finished",
                "total-snapshots": 3,
                "total-comparisons": 3,
                "total-comparisons-unreviewed": 1,
                "created-at": "2024-01-15T10:00:00Z",
                "build-label": "release-v2"
              }
            }
          ]
        }
        """;

        var client = CreateClient(new FakeHttpMessageHandler(HttpStatusCode.OK, json));

        var result = await client.GetBuildsAsync("org/proj");

        Assert.Single(result);
        var build = result[0];
        Assert.Equal("build-1", build.BuildId);
        Assert.Equal("finished", build.State);
        Assert.Equal(3, build.TotalSnapshots);
        Assert.Equal(1, build.UnreviewedComparisons);
        Assert.Equal("release-v2", build.TriggerLabel);
    }

    [Fact]
    public async Task GetBuildAsync_WithEmptyId_ReturnsNull()
    {
        var client = CreateClient(new FakeHttpMessageHandler(HttpStatusCode.OK, "{}"));

        var result = await client.GetBuildAsync("");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetBuildAsync_WhenNotFound_ReturnsNull()
    {
        var client = CreateClient(new FakeHttpMessageHandler(HttpStatusCode.NotFound, "{}"));

        var result = await client.GetBuildAsync("missing-id");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetSnapshotsForBuildAsync_WithEmptyId_ReturnsEmpty()
    {
        var client = CreateClient(new FakeHttpMessageHandler(HttpStatusCode.OK, "{}"));

        var result = await client.GetSnapshotsForBuildAsync("");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetSnapshotsForBuildAsync_ParsesSnapshotsArray()
    {
        const string json = """
        {
          "data": [
            {
              "id": "snap-1",
              "attributes": {
                "name": "Home page",
                "review-state": "approved",
                "diff-url": "https://percy.io/diff/1"
              }
            }
          ]
        }
        """;

        var client = CreateClient(new FakeHttpMessageHandler(HttpStatusCode.OK, json));

        var result = await client.GetSnapshotsForBuildAsync("build-1");

        Assert.Single(result);
        Assert.Equal("snap-1", result[0].SnapshotId);
        Assert.Equal("Home page", result[0].Name);
        Assert.Equal("approved", result[0].ReviewState);
        Assert.Equal("https://percy.io/diff/1", result[0].DiffUrl);
    }

    [Fact]
    public async Task GetBuildWebUrlAsync_ReturnsBuildUrl()
    {
        const string json = """
        {
          "data": {
            "id": "build-42",
            "attributes": {
              "web-url": "https://percy.io/builds/42",
              "state": "finished",
              "total-snapshots": 1,
              "total-comparisons": 1,
              "total-comparisons-unreviewed": 0,
              "created-at": "2024-01-15T10:00:00Z"
            }
          }
        }
        """;

        var client = CreateClient(new FakeHttpMessageHandler(HttpStatusCode.OK, json));

        var url = await client.GetBuildWebUrlAsync("build-42");

        Assert.Equal("https://percy.io/builds/42", url);
    }

    [Fact]
    public async Task GetBuildWebUrlAsync_WhenBuildNotFound_ReturnsEmpty()
    {
        var client = CreateClient(new FakeHttpMessageHandler(HttpStatusCode.NotFound, "{}"));

        var url = await client.GetBuildWebUrlAsync("missing");

        Assert.Equal(string.Empty, url);
    }
}
