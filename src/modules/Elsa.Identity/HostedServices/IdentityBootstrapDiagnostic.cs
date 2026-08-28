using Elsa.Identity.Contracts;
using Elsa.Identity.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Identity.HostedServices;

/// <summary>
/// Reports, at startup, an instance nobody can sign in to: an empty user store with no bootstrap configured.
/// </summary>
/// <remarks>
/// Bootstrapping the first identity is a chicken-and-egg problem — every management endpoint requires a
/// permission, which requires a role, which requires a user. Elsa answers it declaratively: configure
/// <see cref="DefaultAdminUserOptions"/> to seed an admin, or <see cref="AdminApiKeyOptions"/> to accept an
/// out-of-band key. Both work in a deployed environment, and both attach an identity to whatever the caller
/// then does.
///
/// This replaces the localhost permission grant that used to ride on the SecurityRoot policy. That grant
/// trusted network position, which is exactly the signal that stops meaning anything behind a reverse proxy,
/// inside a container, or across a port-forward — and it granted unauthenticated access, so the bootstrap
/// action had no identity to audit. What it did offer was a hint that something needed configuring; without
/// it, an unconfigured instance would answer every request with 403 and no explanation. This says so instead.
/// </remarks>
[UsedImplicitly]
public class IdentityBootstrapDiagnostic(
    IServiceScopeFactory scopeFactory,
    IOptions<DefaultAdminUserOptions> adminUserOptions,
    IOptions<AdminApiKeyOptions> adminApiKeyOptions,
    ILogger<IdentityBootstrapDiagnostic> logger) : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var admin = adminUserOptions.Value;
        var adminUserConfigured = !string.IsNullOrWhiteSpace(admin.AdminUserName) && !string.IsNullOrWhiteSpace(admin.AdminPassword);
        var apiKeyConfigured = !string.IsNullOrWhiteSpace(adminApiKeyOptions.Value.ApiKey);

        if (adminUserConfigured || apiKeyConfigured)
            return;

        try
        {
            using var scope = scopeFactory.CreateScope();
            var userStore = scope.ServiceProvider.GetRequiredService<IUserStore>();

            if ((await userStore.FindManyAsync(new(), cancellationToken)).Any())
                return;

            logger.LogError(
                "No users exist and no identity bootstrap is configured, so nothing can sign in and every " +
                "management endpoint will answer 403. Configure one of: (1) a seeded administrator via " +
                "UseDefaultAdmin(...) or the DefaultAdminUser configuration section, which creates the admin " +
                "role and user at startup and is idempotent; or (2) an admin API key via UseAdminApiKey(...) " +
                "or the AdminApiKey setting. Both work in a deployed environment.");
        }
        catch (Exception e)
        {
            // A store that cannot be read yet is not this check's problem to report; it will surface on its
            // own. Never let a diagnostic take the host down.
            logger.LogDebug(e, "Could not determine whether the user store is empty; skipping the bootstrap check.");
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
