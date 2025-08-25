using System.Threading.Channels;
using Elsa.Logging.Contracts;
using Elsa.Logging.Models;

namespace Elsa.Logging.Services;

/// <inheritdoc />
public class LogEntryQueue : ILogEntryQueue
{
    private readonly Channel<LogEntryInstruction> _channel = Channel.CreateUnbounded<LogEntryInstruction>();

    /// <inheritdoc />
    public async ValueTask EnqueueAsync(LogEntryInstruction instruction)
    {
        await _channel.Writer.WriteAsync(instruction);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<LogEntryInstruction> DequeueAsync()
    {
        while (await _channel.Reader.WaitToReadAsync())
        while (_channel.Reader.TryRead(out var instruction))
            yield return instruction;
    }
}