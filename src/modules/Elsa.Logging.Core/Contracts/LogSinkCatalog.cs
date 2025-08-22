namespace Elsa.Logging.Contracts;

public class LogSinkCatalog(IEnumerable<ILogSinkProvider> providers) : ILogSinkCatalog
{
    private readonly Lazy<Task<List<ILogSink>>> _sinksLazy = new(() => LoadSinksAsync(providers), LazyThreadSafetyMode.ExecutionAndPublication);

    private static async Task<List<ILogSink>> LoadSinksAsync(IEnumerable<ILogSinkProvider> providers, CancellationToken cancellationToken = default)
    {
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