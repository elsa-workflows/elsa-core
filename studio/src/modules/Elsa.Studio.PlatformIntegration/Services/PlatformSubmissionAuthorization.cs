using Elsa.Studio.Authorization;
using Elsa.Studio.Contracts;
using Elsa.Studio.Security.Client;
using Microsoft.Extensions.Logging;

namespace Elsa.Studio.PlatformIntegration.Services;

internal interface IPlatformSubmissionAuthorization
{
    Task<bool> CanSubmitAsync(CancellationToken cancellationToken = default);
}

internal sealed class PlatformSubmissionAuthorization(
    ILogger<PlatformSubmissionAuthorization> logger,
    IBackendApiClientProvider? backend = null) : IPlatformSubmissionAuthorization
{
    private static readonly Permission Deploy = new("workflows/definitions", "deploy");

    public async Task<bool> CanSubmitAsync(CancellationToken cancellationToken = default)
    {
        if (OperatingSystem.IsBrowser() || backend is null)
            return false;

        try
        {
            // Read through the authenticated backend client so deployment never relies on the cached UI snapshot.
            var api = await backend.GetApiAsync<IMePermissionsApi>(cancellationToken);
            var response = await api.GetAsync(cancellationToken);
            var grants = response.Grants.SelectMany(grant => grant.Verbs.Select(verb => new Permission(grant.Resource, verb)));
            return PermissionMatcher.Satisfies(grants, Deploy);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Could not verify caller permission for Platform deployment.");
            return false;
        }
    }
}
