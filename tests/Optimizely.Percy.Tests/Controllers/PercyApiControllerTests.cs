using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Optimizely.Percy.Controllers;
using Optimizely.Percy.Models;
using Optimizely.Percy.Services;

namespace Optimizely.Percy.Tests.Controllers;

public class PercyApiControllerTests
{
    private readonly Mock<IPercySnapshotService> _snapshotServiceMock = new();

    private PercyApiController CreateController() =>
        new PercyApiController(_snapshotServiceMock.Object);

    [Fact]
    public async Task GetBuilds_ReturnsOkWithBuilds()
    {
        var builds = new List<PercyBuildSummary>
        {
            new("b1", "https://percy.io/builds/1", "finished", 5, 5, 0, DateTimeOffset.UtcNow, null)
        };
        _snapshotServiceMock
            .Setup(s => s.GetRecentBuildsAsync(10))
            .ReturnsAsync(builds);

        var controller = CreateController();
        var result = await controller.GetBuilds(10);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var data = Assert.IsAssignableFrom<IReadOnlyList<PercyBuildSummary>>(ok.Value);
        Assert.Single(data);
    }

    [Fact]
    public async Task GetBuildDiff_WithEmptyId_ReturnsBadRequest()
    {
        var controller = CreateController();
        var result = await controller.GetBuildDiff("");

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetBuildDiff_WithValidId_ReturnsOk()
    {
        _snapshotServiceMock
            .Setup(s => s.GetBuildDiffUrlAsync("build-99"))
            .ReturnsAsync("https://percy.io/builds/99");

        var controller = CreateController();
        var result = await controller.GetBuildDiff("build-99");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task TriggerSnapshot_WithNoUrls_ReturnsBadRequest()
    {
        var controller = CreateController();
        var result = await controller.TriggerSnapshot(new ManualSnapshotRequest([], null));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task TriggerSnapshot_WhenSuccessful_ReturnsOk()
    {
        var buildResult = new PercyBuildResult(true, "b1", "https://percy.io/b1", "ok", 3);
        _snapshotServiceMock
            .Setup(s => s.TriggerBatchSnapshotAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<string>()))
            .ReturnsAsync(buildResult);

        var controller = CreateController();
        var result = await controller.TriggerSnapshot(new ManualSnapshotRequest(["/en/home"], "test"));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var data = Assert.IsType<PercyBuildResult>(ok.Value);
        Assert.True(data.Success);
    }

    [Fact]
    public async Task TriggerSnapshot_WhenFailed_Returns500()
    {
        var buildResult = new PercyBuildResult(false, "", "", "Percy CLI failed", 0);
        _snapshotServiceMock
            .Setup(s => s.TriggerBatchSnapshotAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<string>()))
            .ReturnsAsync(buildResult);

        var controller = CreateController();
        var result = await controller.TriggerSnapshot(new ManualSnapshotRequest(["/en/page"], "label"));

        var status = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, status.StatusCode);
    }
}
