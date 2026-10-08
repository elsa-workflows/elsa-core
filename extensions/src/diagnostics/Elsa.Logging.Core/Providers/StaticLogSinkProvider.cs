using Elsa.Logging.Contracts;
using Elsa.Logging.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Logging.Providers;

/// <summary>
/// Provides a static implementation of the <see cref="ILogSinkProvider"/> interface.
/// This provider retrieves log sinks from a predefined configuration.
/// </summary>
public class StaticLogSinkProvider(IOptions<LoggingOptions> options) : ILogSinkProvider
{
    public Task<IEnumerable<ILogSink>> GetLogSinksAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ILogSink>>(options.Value.Sinks);
    }
}