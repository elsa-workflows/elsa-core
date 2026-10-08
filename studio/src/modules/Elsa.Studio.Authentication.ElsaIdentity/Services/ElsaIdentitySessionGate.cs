namespace Elsa.Studio.Authentication.ElsaIdentity.Services;

/// <summary>
/// Serializes changes to the stored ElsaIdentity session within a scope (a Blazor Server circuit or the WASM app), so
/// a token refresh cannot store its response between sign-out's clearing of the session and the page unloading.
/// </summary>
public sealed class ElsaIdentitySessionGate : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>Runs <paramref name="action"/> while no other session change is in progress.</summary>
    public async Task<T> RunAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await action();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _semaphore.Dispose();
}
