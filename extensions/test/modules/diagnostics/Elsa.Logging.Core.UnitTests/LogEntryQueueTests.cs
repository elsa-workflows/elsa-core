using Elsa.Logging.Services;
using Elsa.Logging.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Logging.Core.UnitTests;

public class LogEntryQueueTests
{
    [Fact]
    public async Task EnqueueAndDequeue_ShouldReturnSameLogEntryInstruction()
    {
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
            Assert.Equal(instruction, dequeued);
            break;
        }
    }
}