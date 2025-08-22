using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Logging.Contracts;

public class LogSinkCatalog(IServiceScopeFactory scopeFactory) : ILogSinkCatalog
{
    private readonly Lazy<Task<List<ILogSink>>> _sinksLazy = new(() => LoadSinksAsync(scopeFactory), LazyThreadSafetyMode.ExecutionAndPublication);

    private static async Task<List<ILogSink>> LoadSinksAsync(IServiceScopeFactory scopeFactory, CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var providers = scope.ServiceProvider.GetServices<ILogSinkProvider>();
        var allSinks = new List<ILogSink>();
        foreach (var provider in providers)
        {
            var sinks = await provider.GetLogSinksAsync(cancellationToken);
            allSinks.AddRange(sinks);
        }
        return allSinks;
    }

    public async Task<IEnumerable<ILogSink>> ListAsync(CancellationToken cancellationToken = default)
    {
        var sinks = await _sinksLazy.Value;
        return sinks;
    }

    public async Task<ILogSink?> GetAsync(string name, CancellationToken cancellationToken = default)
    {
        var sinks = await _sinksLazy.Value;
        return sinks.FirstOrDefault(sink => sink.Name == name);
    }
}