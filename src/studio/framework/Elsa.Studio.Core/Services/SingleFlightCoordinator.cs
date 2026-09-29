using Elsa.Studio.Contracts;

namespace Elsa.Studio.Services;

/// <summary>
/// Default implementation of <see cref="ISingleFlightCoordinator"/>.
/// </summary>
public class SingleFlightCoordinator : ISingleFlightCoordinator
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <inheritdoc />
    public async Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            return await action(cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
