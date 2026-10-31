using Elsa.Logging.Models;
using JetBrains.Annotations;

namespace Elsa.Logging.Contracts;

/// <summary>
/// Represents a factory interface for producing log sink implementations, which handle logging based on specific configuration or options.
/// </summary>
public interface ILogSinkFactory
{
    string Type { get; }
}

/// <summary>
/// Defines a factory for creating instances of log sinks designed for specific logging options types.
/// </summary>
public interface ILogSinkFactory<in TOptions> : ILogSinkFactory where TOptions : LogSinkOptions
{
    [UsedImplicitly]
    ILogSink Create(string name, TOptions options);
}