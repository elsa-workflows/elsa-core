using Elsa.Logging.Contracts;
using Microsoft.Extensions.Logging;

namespace Elsa.Logging.Services;

public sealed class LogSinkRouter : ILogSinkRouter
{
    private readonly ILogSinkCatalog _catalog;
    private readonly Lazy<Task<IDictionary<string, ILogSink>>> _lazyTargets;
    private readonly string[] _defaults;

    public LogSinkRouter(ILogSinkCatalog catalog, IEnumerable<string>? defaults = null)
    {
        _catalog = catalog;
        _lazyTargets = new(GetLogSinksAsync, LazyThreadSafetyMode.ExecutionAndPublication);
        _defaults = (defaults ?? []).ToArray();
    }

    public async ValueTask WriteAsync(IEnumerable<string> targetNames, LogLevel level, string message, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
    {
        var targetNamesArray = targetNames as string[] ?? targetNames.ToArray();
        var names = targetNamesArray.Any() ? targetNamesArray : _defaults;
        var uniqueNames = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
        var targets = await _lazyTargets.Value;
        foreach (var n in uniqueNames)
            if (targets.TryGetValue(n, out var t))
                await t.WriteAsync(level, message, properties, cancellationToken);
    }

    private async Task<IDictionary<string, ILogSink>> GetLogSinksAsync()
    {
        var sinks = await _catalog.ListAsync();
        return sinks.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
    }
}