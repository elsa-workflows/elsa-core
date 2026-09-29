using Elsa.Logging.Contracts;
using Elsa.Logging.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Logging.Services;

/// <inheritdoc />
public sealed class LogSinkRouter : ILogSinkRouter
{
    private readonly ILogSinkCatalog _catalog;
    private readonly Lazy<Task<IDictionary<string, ILogSink>>> _lazyTargets;
    private readonly string[] _defaults;

    public LogSinkRouter(ILogSinkCatalog catalog, IOptions<LoggingOptions> options)
    {
        _catalog = catalog;
        _lazyTargets = new(GetLogSinksAsync, LazyThreadSafetyMode.ExecutionAndPublication);
        _defaults = options.Value.Defaults.ToArray();
    }

    public async ValueTask WriteAsync(IEnumerable<string> sinkNames, string name, LogLevel level, string message, object? arguments, IDictionary<string, object?>? attributes = null, CancellationToken cancellationToken = default)
    {
        var targetNamesArray = sinkNames as string[] ?? sinkNames.ToArray();
        var names = targetNamesArray.Any() ? targetNamesArray : _defaults;
        var uniqueNames = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
        var targets = await _lazyTargets.Value;

        foreach (var n in uniqueNames)
            if (targets.TryGetValue(n, out var t))
                await t.WriteAsync(name, level, message, arguments, attributes, cancellationToken);
    }

    private async Task<IDictionary<string, ILogSink>> GetLogSinksAsync()
    {
        var sinks = await _catalog.ListAsync();
        return sinks.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
    }
}