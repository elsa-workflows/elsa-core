using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Logging.Contracts;

public interface ILogSinkFactory<in TOptions> where TOptions : SinkOptions.SinkOptions
{
    string Type { get; }
    ILogSink Create(string name, TOptions options, IServiceProvider sp);
}