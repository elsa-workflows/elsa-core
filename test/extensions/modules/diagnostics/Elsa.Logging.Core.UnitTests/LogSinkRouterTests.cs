using Elsa.Logging.Services;
using Elsa.Logging.Contracts;
using Elsa.Logging.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Elsa.Logging.Core.UnitTests;

public class LogSinkRouterTests
{
    [Fact]
    public async Task WriteAsync_ShouldCallSinkWithLogEntry()
    {
        var sinkMock = new Mock<ILogSink>();
        sinkMock.Setup(s => s.Name).Returns("TestSink");
        sinkMock.Setup(s => s.WriteAsync(
            It.IsAny<string>(),
            It.IsAny<LogLevel>(),
            It.IsAny<string>(),
            It.IsAny<object>(),
            It.IsAny<IDictionary<string, object?>>(),
            It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);

        var catalogMock = new Mock<ILogSinkCatalog>();
        catalogMock.Setup(c => c.ListAsync(CancellationToken.None)).ReturnsAsync(new List<ILogSink>
        {
            sinkMock.Object
        });

        var optionsMock = new Mock<IOptions<LoggingOptions>>();
        optionsMock.Setup(o => o.Value).Returns(new LoggingOptions
        {
            Defaults = ["TestSink"]
        });

        var router = new LogSinkRouter(catalogMock.Object, optionsMock.Object);
        await router.WriteAsync([
            "TestSink"
        ], "TestLogger", LogLevel.Information, "Test message", null, null);
        sinkMock.Verify(s => s.WriteAsync(
            "TestLogger",
            LogLevel.Information,
            "Test message",
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}