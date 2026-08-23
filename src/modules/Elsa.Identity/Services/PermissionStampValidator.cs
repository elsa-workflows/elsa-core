using System.Security.Claims;
using Elsa.Identity.Contracts;
using Elsa.Identity.Models;
using Elsa.Identity.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Elsa.Identity.Services;

/// <summary>Rejects a token whose permission stamp no longer matches the user's current grants.</summary>
[UsedImplicitly]
public class PermissionStampValidator(
    IUserProvider userProvider,
    IPermissionStampCalculator calculator,
    IMemoryCache cache,
    IOptions<PermissionStampOptions> options)
{
    /// <summary>
    /// Whether <paramref name="principal"/> still carries a current stamp. Returns <c>true</c> when the
    /// stamp is disabled, and when a token predates the feature being turned on -- an absent stamp is not
    /// treated as a mismatch, so enabling it does not sign everyone out.
    /// </summary>
    public async ValueTask<bool> IsCurrentAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        if (!options.Value.IsEnabled)
            return true;

        var presented = principal.FindFirst(PermissionStampCalculator.ClaimType)?.Value;

        if (string.IsNullOrWhiteSpace(presented))
            return true;

        var userName = principal.Identity?.Name;

        if (string.IsNullOrWhiteSpace(userName))
            return true;

        var current = await cache.GetOrCreateAsync($"elsa:permission-stamp:{userName}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = options.Value.CacheLifetime;

            var user = await userProvider.FindAsync(new UserFilter { Name = userName }, cancellationToken);

            return user is null ? null : await calculator.ComputeAsync(user, cancellationToken);
        });

        // A user that cannot be resolved is not evidence of a stale token; leave that to authentication.
        return current is null || string.Equals(current, presented, StringComparison.Ordinal);
    }
}
