using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;

namespace Elsa.Identity.Providers;

/// <summary>
/// Represents a user provider that always returns a single admin user. This is useful for development purposes.
/// </summary>
public class AdminUserProvider : IUserProvider
{
    private readonly User _adminUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminUserProvider"/> class.
    /// </summary>
    public AdminUserProvider(ISecretHasher secretHasher)
    {
        var hashedSecret = secretHasher.HashSecret("password");

        _adminUser = new User
        {
            Id = "admin",
            Name = "admin",
            HashedPassword = hashedSecret.EncodeSecret(),
            HashedPasswordSalt = hashedSecret.EncodeSalt(),
            Roles = { "admin" }
        };
    }

    /// <inheritdoc />
    public Task<User?> FindAsync(UserFilter filter, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_adminUser)!;
    }
}