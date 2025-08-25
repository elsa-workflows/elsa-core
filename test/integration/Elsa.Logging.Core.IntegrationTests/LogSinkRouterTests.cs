using Elsa.Logging.Services;
using Elsa.Logging.Sinks;
using Elsa.Logging.Models;
using Elsa.Logging.Contracts;
using Elsa.Logging.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Elsa.Logging.Core.IntegrationTests;

public class LogSinkRouterTests
{
    [Fact]
    public async Task LogEntryInstruction_ShouldFlowThroughQueueAndRouterToSink()
    {
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        var loggerMock = new Mock<ILogger>();
        loggerFactoryMock.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);
        loggerMock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        var sink = new LoggerSink("TestSink", loggerFactoryMock.Object);
        var catalogMock = new Mock<ILogSinkCatalog>();
        catalogMock.Setup(c => c.ListAsync(CancellationToken.None)).ReturnsAsync(new List<ILogSink>
        {
            sink
        });
        var optionsMock = new Mock<IOptions<LoggingOptions>>();
        optionsMock.Setup(o => o.Value).Returns(new LoggingOptions
        {
            Defaults = ["TestSink"]
        });
        var router = new LogSinkRouter(catalogMock.Object, optionsMock.Object);
        var queue = new LogEntryQueue();
        var instruction = new LogEntryInstruction
        {
            SinkNames = ["TestSink"],
            Category = "TestLogger",
            Level = LogLevel.Information,
            Message = "Test message"
        };
            
        await queue.EnqueueAsync(instruction);
        await foreach (var dequeued in queue.DequeueAsync())
        {
            await router.WriteAsync(dequeued.SinkNames, dequeued.Category, dequeued.Level, dequeued.Message, dequeued.Arguments, dequeued.Attributes);
            break;
        }

        loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            0,
            null,
            null,
            It.IsAny<Func<object, Exception, string>>()!), Times.Once);
    }
}