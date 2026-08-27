using Elsa.Common.Multitenancy;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Workflows;

namespace Elsa.Identity.Services;

/// <summary>
/// Default implementation of <see cref="IUserManager"/>.
/// </summary>
public class UserManager(
    IIdentityGenerator identityGenerator,
    ISecretGenerator secretGenerator,
    ISecretHasher secretHasher,
    IUserStore userStore,
    ITenantAccessor tenantAccessor)
    : IUserManager
{
    /// <inheritdoc />
    public async Task<CreateUserResult> CreateUserAsync(
        string name,
        string? password = null,
        ICollection<string>? roles = null,
        CancellationToken cancellationToken = default)
    {
        var id = identityGenerator.GenerateId();
        var plainTextPassword = string.IsNullOrWhiteSpace(password) ? secretGenerator.Generate() : password.Trim();
        var hashedPassword = secretHasher.HashSecret(plainTextPassword);

        var user = new User
        {
            Id = id,
            Name = name,
            // Set explicitly rather than relying on the Entity Framework saving handler, which does not run
            // on the in-memory path and left users unassigned there.
            TenantId = tenantAccessor.TenantId,
            Roles = roles ?? new List<string>(),
            HashedPassword = hashedPassword.EncodeSecret(),
            HashedPasswordSalt = hashedPassword.EncodeSalt()
        };

        await userStore.SaveAsync(user, cancellationToken);

        return new CreateUserResult(user, plainTextPassword);
    }
}
