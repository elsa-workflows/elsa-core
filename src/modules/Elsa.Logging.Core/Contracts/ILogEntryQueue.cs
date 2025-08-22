using Elsa.Logging.Models;

namespace Elsa.Logging.Contracts;

public interface ILogEntryQueue
{
    ValueTask EnqueueAsync(LogEntryInstruction instruction);
    IAsyncEnumerable<LogEntryInstruction> DequeueAsync();
}
