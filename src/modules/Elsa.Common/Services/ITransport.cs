namespace Elsa.Common.Services;

public interface ITransport<in T>
{
    public Task SendAsync(T message, CancellationToken cancellationToken);
}