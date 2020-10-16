using NodaTime;

namespace Elsa.Services.Models
{
    public interface IExecutionLogEntry
    {
        string ActivityId { get; }
        Instant Timestamp { get; }
    }
}