using Elsa.Logging.Core.UnitTests.Helpers;
using Elsa.Logging.Sinks;
using Microsoft.Extensions.Logging;

namespace Elsa.Logging.Core.UnitTests;

public class LoggerSinkTests
{
    [Fact]
    public async Task WriteAsync_ShouldLogMessage()
    {
        var testLogger = new TestLogger();
        var loggerFactory = new TestLoggerFactory(testLogger);
        var sink = new LoggerSink("TestLogger", loggerFactory);
        await sink.WriteAsync("TestLogger", LogLevel.Information, "Test message", null, null);
        Assert.Single(testLogger.Calls);
        var call = testLogger.Calls[0];
        Assert.Equal(LogLevel.Information, call.level);
    }
}