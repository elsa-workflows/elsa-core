using System.Net;
using Elsa.Studio.Contracts;
using Elsa.Studio.Security.Client;
using Elsa.Studio.Security.Contracts;
using Elsa.Studio.Security.Models;
using Microsoft.Extensions.Logging;
using Refit;

namespace Elsa.Studio.Security.Services;

/// <summary>
/// Loads the current caller's effective permissions once per Studio scope.
/// </summary>
public sealed class IdentityPermissionContext(
    IBackendApiClientProvider apiClientProvider,
    ILogger<IdentityPermissionContext> logger) : IIdentityPermissionContext
{
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private IdentityPermissionSnapshot? _snapshot;

    public async Task<IdentityPermissionSnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_snapshot != null)
            return _snapshot;

        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            if (_snapshot != null)
                return _snapshot;

            try
            {
                var api = await apiClientProvider.GetApiAsync<IMePermissionsApi>(cancellationToken);
                var response = await api.GetAsync(cancellationToken);
                var grants = response.Grants
                    .GroupBy(x => x.Resource, StringComparer.Ordinal)
                    .ToDictionary(
                        group => group.Key,
                        group => (IReadOnlySet<string>)group
                            .SelectMany(x => x.Verbs)
                            .ToHashSet(StringComparer.Ordinal),
                        StringComparer.Ordinal);

                _snapshot = new IdentityPermissionSnapshot(IdentityPermissionSnapshotState.Ready, grants);
            }
            catch (ApiException exception) when (exception.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                _snapshot = IdentityPermissionSnapshot.Forbidden;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("Loading the current Identity permissions timed out");
                _snapshot = IdentityPermissionSnapshot.Unavailable;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Loading the current Identity permissions failed");
                _snapshot = IdentityPermissionSnapshot.Unavailable;
            }

            return _snapshot;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public void Invalidate() => _snapshot = null;
}
