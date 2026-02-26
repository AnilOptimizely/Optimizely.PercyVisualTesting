using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Optimizely.Percy.Configuration;
using Optimizely.Percy.Controllers;
using Optimizely.Percy.Models;
using Optimizely.Percy.Services;

namespace Optimizely.Percy.Tests.Controllers;

public class PercyAdminControllerTests
{
    private readonly Mock<IPercySnapshotService> _snapshotServiceMock = new();

    private PercyAdminController CreateController(PercyOptions? options = null)
    {
        var opts = Options.Create(options ?? new PercyOptions { Token = "test-token", ProjectSlug = "org/proj" });
        return new PercyAdminController(_snapshotServiceMock.Object, opts);
    }

    [Fact]
    public async Task Index_WhenConfigured_ReturnsViewWithBuilds()
    {
        var builds = new List<PercyBuildSummary>
        {
            new("b1", "https://percy.io/builds/1", "finished", 3, 3, 0, DateTimeOffset.UtcNow, null)
        };
        _snapshotServiceMock
            .Setup(s => s.GetRecentBuildsAsync(10))
            .ReturnsAsync(builds);

        var controller = CreateController();
        var result = await controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<PercyDashboardViewModel>(viewResult.Model);
        Assert.True(vm.IsConfigured);
        Assert.Single(vm.RecentBuilds);
    }

    [Fact]
    public async Task Index_WhenNotConfigured_DoesNotCallApiClient()
    {
        var controller = CreateController(new PercyOptions { Token = "" });
        var result = await controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<PercyDashboardViewModel>(viewResult.Model);
        Assert.False(vm.IsConfigured);
        _snapshotServiceMock.Verify(s => s.GetRecentBuildsAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Builds_ReturnsJsonResult()
    {
        var builds = new List<PercyBuildSummary>
        {
            new("b1", "https://percy.io/builds/1", "finished", 2, 2, 0, DateTimeOffset.UtcNow, "label1")
        };
        _snapshotServiceMock
            .Setup(s => s.GetRecentBuildsAsync(5))
            .ReturnsAsync(builds);

        var controller = CreateController();
        var result = await controller.Builds(5);

        var json = Assert.IsType<JsonResult>(result);
        var data = Assert.IsAssignableFrom<IReadOnlyList<PercyBuildSummary>>(json.Value);
        Assert.Single(data);
    }

    [Fact]
    public async Task Trigger_WhenNoUrls_ReturnsBadRequest()
    {
        var controller = CreateController();
        var request = new ManualSnapshotRequest([], null);

        var result = await controller.Trigger(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Trigger_WhenValidRequest_ReturnsJson()
    {
        var buildResult = new PercyBuildResult(true, "", "", "ok", 2);
        _snapshotServiceMock
            .Setup(s => s.TriggerBatchSnapshotAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<string>()))
            .ReturnsAsync(buildResult);

        var controller = CreateController();
        var request = new ManualSnapshotRequest(["/en/home", "/en/about"], "test");

        var result = await controller.Trigger(request);

        var json = Assert.IsType<JsonResult>(result);
        var data = Assert.IsType<PercyBuildResult>(json.Value);
        Assert.True(data.Success);
    }

    [Fact]
    public async Task Status_ReturnsJsonWithConfiguration()
    {
        var controller = CreateController();
        var result = controller.Status();

        var json = Assert.IsType<JsonResult>(result);
        Assert.NotNull(json.Value);
    }
}
