using Microsoft.AspNetCore.Components.Authorization;

namespace Elsa.Studio.ExternalAuthentication.Services;

public interface IExternalAuthenticationPermissionService
{
    ValueTask<bool> HasAsync(string permission, CancellationToken cancellationToken = default);
    ValueTask<IReadOnlySet<string>> ListAsync(CancellationToken cancellationToken = default);
}

/// <summary>Reads Elsa's authoritative <c>permissions</c> claims solely to tailor Studio affordances.</summary>
public sealed class ExternalAuthenticationPermissionService(AuthenticationStateProvider authenticationStateProvider) : IExternalAuthenticationPermissionService
{
    /// <summary>Elsa's known-empty sentinel (<c>PermissionNames.None</c>). Not a grant.</summary>
    private const string EmptySetSentinel = "none";

    public async ValueTask<bool> HasAsync(string permission, CancellationToken cancellationToken = default)
    {
        var permissions = await ListAsync(cancellationToken);
        return permissions.Contains("*") || permissions.Contains(permission);
    }

    public async ValueTask<IReadOnlySet<string>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = (await authenticationStateProvider.GetAuthenticationStateAsync()).User;
        return user.FindAll("permissions")
            .Select(claim => claim.Value)
            .Where(value => !string.Equals(value, EmptySetSentinel, StringComparison.Ordinal))
            .ToHashSet(StringComparer.Ordinal);
    }
}
