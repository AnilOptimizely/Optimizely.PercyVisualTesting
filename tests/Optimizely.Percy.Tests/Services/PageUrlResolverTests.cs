using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using EPiServer.Core;
using EPiServer.Web.Routing;
using Optimizely.Percy.Services;

namespace Optimizely.Percy.Tests.Services;

public class PageUrlResolverTests
{
    private readonly Mock<IUrlResolver> _urlResolverMock = new();
    private readonly Mock<ILogger<PageUrlResolver>> _loggerMock = new();

    private PageUrlResolver CreateResolver() =>
        new PageUrlResolver(_urlResolverMock.Object, _loggerMock.Object);

    [Fact]
    public void ResolveUrl_WhenContentIsNull_ReturnsNull()
    {
        var resolver = CreateResolver();

        var result = resolver.ResolveUrl(null!);

        Assert.Null(result);
    }

    [Fact]
    public void ResolveUrl_WhenUrlResolverReturnsUrl_ReturnsUrl()
    {
        var contentMock = new Mock<IContent>();
        contentMock.Setup(c => c.Name).Returns("Home");
        contentMock.Setup(c => c.ContentLink).Returns(new ContentReference(1));
        _urlResolverMock.Setup(r => r.GetUrl(It.IsAny<IContent>())).Returns("/en/home");

        var resolver = CreateResolver();
        var result = resolver.ResolveUrl(contentMock.Object);

        Assert.Equal("/en/home", result);
    }

    [Fact]
    public void ResolveUrl_WhenUrlResolverReturnsEmpty_ReturnsNull()
    {
        var contentMock = new Mock<IContent>();
        contentMock.Setup(c => c.Name).Returns("Media");
        contentMock.Setup(c => c.ContentLink).Returns(new ContentReference(2));
        _urlResolverMock.Setup(r => r.GetUrl(It.IsAny<IContent>())).Returns("");

        var resolver = CreateResolver();
        var result = resolver.ResolveUrl(contentMock.Object);

        Assert.Null(result);
    }

    [Fact]
    public void ResolveUrl_WhenUrlResolverThrows_ReturnsNull()
    {
        var contentMock = new Mock<IContent>();
        contentMock.Setup(c => c.Name).Returns("Error Page");
        contentMock.Setup(c => c.ContentLink).Returns(new ContentReference(3));
        _urlResolverMock.Setup(r => r.GetUrl(It.IsAny<IContent>())).Throws(new InvalidOperationException("Test"));

        var resolver = CreateResolver();
        var result = resolver.ResolveUrl(contentMock.Object);

        Assert.Null(result);
    }

    [Fact]
    public void ResolveUrl_WhenUrlResolverReturnsWhitespace_ReturnsNull()
    {
        var contentMock = new Mock<IContent>();
        contentMock.Setup(c => c.Name).Returns("Hidden");
        contentMock.Setup(c => c.ContentLink).Returns(new ContentReference(4));
        _urlResolverMock.Setup(r => r.GetUrl(It.IsAny<IContent>())).Returns("   ");

        var resolver = CreateResolver();
        var result = resolver.ResolveUrl(contentMock.Object);

        Assert.Null(result);
    }
}
