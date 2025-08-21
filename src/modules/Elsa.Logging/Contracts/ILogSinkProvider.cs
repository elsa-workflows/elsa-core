namespace Elsa.Logging.Contracts;

public interface ILogSinkProvider
{
    Task<IEnumerable<ILogSink>> GetLogSinksAsync(CancellationToken cancellationToken = default);
}