using Elsa.Common.Multitenancy;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Workflows;

namespace Elsa.Identity.Services;

/// <summary>
/// Default implementation of <see cref="IUserManager"/>.
/// </summary>
public class UserManager : IUserManager
{
    private readonly IIdentityGenerator _identityGenerator;
    private readonly ISecretGenerator _secretGenerator;
    private readonly ISecretHasher _secretHasher;
    private readonly IUserStore _userStore;
    private readonly ITenantAccessor _tenantAccessor;

    public UserManager(
        IIdentityGenerator identityGenerator,
        ISecretGenerator secretGenerator,
        ISecretHasher secretHasher,
        IUserStore userStore,
        ITenantAccessor tenantAccessor)
    {
        _identityGenerator = identityGenerator;
        _secretGenerator = secretGenerator;
        _secretHasher = secretHasher;
        _userStore = userStore;
        _tenantAccessor = tenantAccessor;
    }

    /// <inheritdoc />
    public async Task<CreateUserResult> CreateUserAsync(
        string name,
        string? password = null,
        ICollection<string>? roles = null,
        CancellationToken cancellationToken = default)
    {
        var id = _identityGenerator.GenerateId();
        var plainTextPassword = string.IsNullOrWhiteSpace(password) ? _secretGenerator.Generate() : password.Trim();
        var hashedPassword = _secretHasher.HashSecret(plainTextPassword);

        var user = new User
        {
            Id = id,
            Name = name,
            // Set explicitly rather than relying on the Entity Framework saving handler, which does not run
            // on the in-memory path and left users unassigned there.
            TenantId = _tenantAccessor.TenantId,
            Roles = roles ?? new List<string>(),
            HashedPassword = hashedPassword.EncodeSecret(),
            HashedPasswordSalt = hashedPassword.EncodeSalt()
        };

        await _userStore.SaveAsync(user, cancellationToken);

        return new CreateUserResult(user, plainTextPassword);
    }
}
