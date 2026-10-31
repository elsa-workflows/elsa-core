using Elsa.Logging.Services;
using Elsa.Logging.Sinks;
using Elsa.Logging.Models;
using Elsa.Logging.Contracts;
using Elsa.Logging.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Elsa.Logging.Core.IntegrationTests.Helpers;
using Moq;

namespace Elsa.Logging.Core.IntegrationTests;

public class LogSinkRouterTests
{
    [Fact]
    public async Task LogEntryInstruction_ShouldFlowThroughQueueAndRouterToSink()
    {
        var testLogger = new TestLogger();
        var loggerFactory = new TestLoggerFactory(testLogger);
        var sink = new LoggerSink("TestSink", loggerFactory);
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

        // Assert that Log was called once with expected parameters
        Assert.Single(testLogger.Calls);
        var call = testLogger.Calls[0];
        Assert.Equal(LogLevel.Information, call.level);
        Assert.Null(call.exception);
    }
}