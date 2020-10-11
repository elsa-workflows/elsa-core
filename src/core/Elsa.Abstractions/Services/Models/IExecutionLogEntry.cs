using NodaTime;

namespace Elsa.Services.Models
{
    public interface IExecutionLogEntry
    {
        IActivity Activity { get; }
        Instant Timestamp { get; }
    }
}