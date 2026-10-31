using System.Collections.Concurrent;

namespace Elsa.Studio.ExternalAuthentication.BlazorServer.Services;

/// <summary>
/// Coordinates a broker refresh across concurrent Server requests. A rotating credential is exchanged once and
/// every request that observed that same credential receives the resulting token set rather than replaying it.
/// </summary>
public sealed class ServerExternalAuthenticationRefreshCoordinator
{
    private readonly ConcurrentDictionary<string, Task<object>> _flights = new(StringComparer.Ordinal);

    public async Task<T> RunAsync<T>(string key, Func<Task<T>> refresh)
    {
        var task = _flights.GetOrAdd(key, _ => ExecuteAsync(refresh));
        try
        {
            return (T)await task;
        }
        finally
        {
            if (task.IsCompleted && _flights.TryGetValue(key, out var active) && ReferenceEquals(active, task))
                _flights.TryRemove(key, out _);
        }
    }

    private static async Task<object> ExecuteAsync<T>(Func<Task<T>> refresh) => (await refresh())!;
}
