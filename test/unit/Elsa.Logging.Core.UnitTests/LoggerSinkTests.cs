using Elsa.Logging.Sinks;
using Microsoft.Extensions.Logging;
using Moq;

namespace Elsa.Logging.Core.UnitTests;

public class LoggerSinkTests
{
    [Fact]
    public async Task WriteAsync_ShouldLogMessage()
    {
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        var loggerMock = new Mock<ILogger>();
        loggerFactoryMock.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);
        loggerMock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        var sink = new LoggerSink("TestLogger", loggerFactoryMock.Object);
        await sink.WriteAsync("TestLogger", LogLevel.Information, "Test message", null, null);
        loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            0,
            null,
            null,
            It.IsAny<Func<object, Exception, string>>()), Times.Once);
    }
}