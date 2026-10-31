using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Identity.Providers;

/// <summary>
/// Represents a user provider that returns a single explicitly configured admin user. This is useful for development purposes.
/// </summary>
public class AdminUserProvider : IUserProvider
{
    private readonly User? _adminUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminUserProvider"/> class.
    /// </summary>
    [Obsolete("Use the options-based constructor. The built-in admin user is disabled unless explicitly configured.")]
    public AdminUserProvider(ISecretHasher secretHasher) : this(secretHasher, Microsoft.Extensions.Options.Options.Create(new AdminUserProviderOptions()))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminUserProvider"/> class.
    /// </summary>
    public AdminUserProvider(ISecretHasher secretHasher, IOptions<AdminUserProviderOptions> options)
    {
        var providerOptions = options.Value;
        if (string.IsNullOrWhiteSpace(providerOptions.UserName) || string.IsNullOrWhiteSpace(providerOptions.Password))
            return;

        var hashedSecret = secretHasher.HashSecret(providerOptions.Password);

        _adminUser = new User
        {
            Id = providerOptions.UserId,
            Name = providerOptions.UserName,
            HashedPassword = hashedSecret.EncodeSecret(),
            HashedPasswordSalt = hashedSecret.EncodeSalt(),
            Roles = providerOptions.Roles.ToList()
        };
    }

    /// <inheritdoc />
    public Task<User?> FindAsync(UserFilter filter, CancellationToken cancellationToken = default)
    {
        if (_adminUser == null)
            return Task.FromResult<User?>(null);

        if (filter.Id != null && filter.Id != _adminUser.Id)
            return Task.FromResult<User?>(null);

        if (filter.Name != null && filter.Name != _adminUser.Name)
            return Task.FromResult<User?>(null);

        return Task.FromResult<User?>(_adminUser);
    }
}
