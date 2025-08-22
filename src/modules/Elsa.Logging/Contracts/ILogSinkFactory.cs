using JetBrains.Annotations;

namespace Elsa.Logging.Contracts;

public interface ILogSinkFactory
{
    string Type { get; }
}

public interface ILogSinkFactory<in TOptions> : ILogSinkFactory where TOptions : SinkOptions.SinkOptions
{
    [UsedImplicitly]
    ILogSink Create(string name, TOptions options);
}