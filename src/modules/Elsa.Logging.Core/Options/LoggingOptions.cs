using Elsa.Logging.Contracts;

namespace Elsa.Logging.Options;

public class LoggingOptions
{
    /// <summary>
    /// Default sinks.
    /// </summary>
    public ICollection<string> Defaults { get; set; } = new List<string>();
    
    /// <summary>
    /// Sinks registered by the host. To register sinks from configuration, use the <see cref="ILogSinkFactory"/> infrastucture.
    /// </summary>
    public ICollection<ILogSink> Sinks { get; set; } = new List<ILogSink>();
}