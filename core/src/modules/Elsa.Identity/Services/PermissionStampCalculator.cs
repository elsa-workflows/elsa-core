using System.Security.Cryptography;
using System.Text;
using Elsa.Extensions;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using JetBrains.Annotations;

namespace Elsa.Identity.Services;

/// <summary>Computes a stamp that changes whenever a user's effective grants change.</summary>
public interface IPermissionStampCalculator
{
    /// <summary>The current stamp for <paramref name="user"/>.</summary>
    ValueTask<string> ComputeAsync(User user, CancellationToken cancellationToken = default);
}

/// <inheritdoc />
/// <remarks>
/// The stamp is <em>derived</em> from the user's roles and those roles' permissions rather than stored as
/// a counter on the user. That is deliberate: a stored counter would change the Identity schema and
/// require migrations across all five EF providers, which would make revocation tightening depend on the
/// tenancy milestone. A derived stamp needs no schema, and every node computes the same value from the
/// same store without any cross-node invalidation -- which matters, because Elsa has none.
///
/// It changes when a role is added to or removed from the user, and when any held role's permissions
/// change. It does not change when an unrelated role changes, so it is no broader than it needs to be.
/// </remarks>
[UsedImplicitly]
public class PermissionStampCalculator(IRoleProvider roleProvider) : IPermissionStampCalculator
{
    /// <summary>The claim carrying the stamp issued with a token.</summary>
    public const string ClaimType = "elsa:permission_stamp";

    /// <inheritdoc />
    public async ValueTask<string> ComputeAsync(User user, CancellationToken cancellationToken = default)
    {
        var roles = (await roleProvider.FindByIdsAsync(user.Roles, cancellationToken)).ToList();

        var material = string.Join(
            "\n",
            roles
                .OrderBy(x => x.Id, StringComparer.Ordinal)
                .Select(role => $"{role.Id}={string.Join(",", role.Permissions.OrderBy(x => x, StringComparer.Ordinal))}"));

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));

        return Convert.ToHexString(hash)[..16];
    }
}
