using Elsa.Logging.Contracts;

namespace Elsa.Logging.Options;

public class LoggingOptions
{
    public ICollection<Type> SinkTypes { get; set; } = new List<Type>();
    public ICollection<ILogSink> Sinks { get; set; } = new List<ILogSink>();
}