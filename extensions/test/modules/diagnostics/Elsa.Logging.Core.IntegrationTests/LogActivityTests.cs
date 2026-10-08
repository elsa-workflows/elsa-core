using System.Collections.Immutable;
using Elsa.Logging.Activities;
using Elsa.Logging.Contracts;
using Elsa.Logging.Extensions;
using Elsa.Logging.Models;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Elsa.Logging.Core.IntegrationTests;

public class LogActivityTests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public LogActivityTests(ITestOutputHelper testOutputHelper)
    {
        var applicationBuilder = new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .ConfigureElsa(elsa =>
        {
            elsa.Services.AddSingleton<ILogEntryQueue, TestLogEntryQueue>();
            elsa.UseLoggingFramework();
        });
        _services = applicationBuilder.Build();
    }

    [Fact(DisplayName = "The Log activity logs the expected message")]
    public async Task Test1()
    {
        var activity = new Log("Hello {Name}", LogLevel.Debug);
        await _services.RunActivityAsync(activity);
        var logEntryQueue = _services.GetRequiredService<ILogEntryQueue>();
        var logEntry = await logEntryQueue.DequeueAsync().FirstAsync();
        Assert.Equal("Hello {Name}", logEntry.Message);
        Assert.Equal(LogLevel.Debug, logEntry.Level);
        Assert.Equal("Process", logEntry.Category);
    }
}

public class TestLogEntryQueue : ILogEntryQueue
{
    private IImmutableQueue<LogEntryInstruction> _queue = ImmutableQueue<LogEntryInstruction>.Empty;
    public ValueTask EnqueueAsync(LogEntryInstruction instruction)
    {
        _queue = _queue.Enqueue(instruction);
        return ValueTask.CompletedTask;
    }

    public async IAsyncEnumerable<LogEntryInstruction> DequeueAsync()
    {
        while (!_queue.IsEmpty)
        {
            _queue = _queue.Dequeue(out var instruction);
            yield return instruction;
        }
    }
}