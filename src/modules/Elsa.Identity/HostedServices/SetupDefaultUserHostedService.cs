using Elsa.Identity.Entities;
using Elsa.Identity.Options;
using Elsa.Identity.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Elsa.Identity.HostedServices;

/// <summary>
/// Adds a default admin user to the system if <see cref="IdentityOptions.EnableDefaultAdmin"/> is set to true.
/// </summary>
public class SetupDefaultUserHostedService : IHostedService
{
    private const string DefaultUserName = "Admin";
    private const string DefaultPassword = "password";

    private readonly IUserStore _userStore;
    private readonly IPasswordHasher _passwordHasher;

    /// <summary>
    /// The constructor.
    /// </summary>
    public SetupDefaultUserHostedService(IUserStore userStore, IPasswordHasher passwordHasher)
    {
        _userStore = userStore;
        _passwordHasher = passwordHasher;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var defaultUser = await _userStore.FindAsync(DefaultUserName, cancellationToken);

        if (defaultUser != null)
            return;

        defaultUser = new User
        {
            Id = Guid.Empty.ToString("N"),
            Name = DefaultUserName,
            Roles = { new Role { Id = Guid.Empty.ToString("N"), Name = "Admin", Permissions = new[] { PermissionNames.All } } },
            HashedPassword = _passwordHasher.HashPassword(DefaultPassword)
        };

        await _userStore.SaveAsync(defaultUser, cancellationToken);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}