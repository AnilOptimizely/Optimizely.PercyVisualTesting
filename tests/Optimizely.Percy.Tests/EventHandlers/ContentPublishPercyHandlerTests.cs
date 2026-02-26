using Xunit;
using Moq;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using EPiServer.Core;
using Optimizely.Percy.Configuration;
using Optimizely.Percy.EventHandlers;
using Optimizely.Percy.Services;

namespace Optimizely.Percy.Tests.EventHandlers;

public class ContentPublishPercyHandlerTests
{
    [Fact]
    public void Initialize_SubscribesToPublishedContent()
    {
        var contentEventsMock = new Mock<IContentEvents>();
        var snapshotServiceMock = new Mock<IPercySnapshotService>();
        var options = Options.Create(new PercyOptions { Token = "t", TriggerOnPublish = true });

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IContentEvents))).Returns(contentEventsMock.Object);
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IOptions<PercyOptions>))).Returns(options);
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IPercySnapshotService))).Returns(snapshotServiceMock.Object);

        var engine = new EPiServer.Framework.InitializationEngine { ServiceLocator = serviceProviderMock.Object };
        var handler = new ContentPublishPercyHandler();

        handler.Initialize(engine);

        contentEventsMock.VerifyAdd(e => e.PublishedContent += It.IsAny<EventHandler<ContentEventArgs>>(), Times.Once);
    }

    [Fact]
    public void Uninitialize_UnsubscribesFromPublishedContent()
    {
        var contentEventsMock = new Mock<IContentEvents>();
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IContentEvents))).Returns(contentEventsMock.Object);

        var engine = new EPiServer.Framework.InitializationEngine { ServiceLocator = serviceProviderMock.Object };
        var handler = new ContentPublishPercyHandler();

        handler.Initialize(engine);
        handler.Uninitialize(engine);

        contentEventsMock.VerifyRemove(e => e.PublishedContent -= It.IsAny<EventHandler<ContentEventArgs>>(), Times.Once);
    }

    [Fact]
    public void Initialize_WhenContentEventsNotRegistered_DoesNotThrow()
    {
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IContentEvents))).Returns((object?)null);

        var engine = new EPiServer.Framework.InitializationEngine { ServiceLocator = serviceProviderMock.Object };
        var handler = new ContentPublishPercyHandler();

        var ex = Record.Exception(() => handler.Initialize(engine));
        Assert.Null(ex);
    }
}
