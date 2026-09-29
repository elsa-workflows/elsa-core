using Elsa.Studio.Authorization;
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
    public async ValueTask<bool> HasAsync(string permission, CancellationToken cancellationToken = default)
    {
        // Unlike the shell's menu gating, this fails closed: without grants the affordances stay hidden.
        if (!Permission.TryParse(permission, out var required))
            return false;

        var grants = (await ListAsync(cancellationToken)).Select(x => Permission.TryParse(x, out var grant) ? grant : (Permission?)null).OfType<Permission>();
        return PermissionMatcher.Satisfies(grants, required);
    }

    public async ValueTask<IReadOnlySet<string>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = (await authenticationStateProvider.GetAuthenticationStateAsync()).User;
        return user.FindAll("permissions").Select(claim => claim.Value).ToHashSet(StringComparer.Ordinal);
    }
}
