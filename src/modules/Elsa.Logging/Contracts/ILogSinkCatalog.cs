namespace Elsa.Logging.Contracts;

public interface ILogSinkCatalog
{
    Task<IEnumerable<ILogSink>> ListAsync(CancellationToken cancellationToken = default);
    Task<ILogSink?> GetAsync(string id, CancellationToken cancellationToken = default);
}