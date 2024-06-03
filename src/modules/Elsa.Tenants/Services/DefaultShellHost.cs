using Elsa.Common;
using Elsa.Tenants.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Tenants.Services;

/// A default shell host implementation that creates and manages shells for the main application and for each isolated tenant.
public class DefaultShellHost(
    IShellFactory shellFactory,
    IServiceScopeFactory scopeFactory,
    IApplicationServicesAccessor applicationServicesAccessor,
    IServiceProvider applicationServiceProvider)
    : IShellHost
{
    /// <inheritdoc />
    public Shell ApplicationShell { get; } = new(applicationServicesAccessor.ApplicationServices, applicationServiceProvider);

    private IDictionary<string, Shell> TenantShells { get; set; } = new Dictionary<string, Shell>();

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var tenantsProvider = scope.ServiceProvider.GetRequiredService<ITenantsProvider>();
        var tenants = (await tenantsProvider.ListAsync(cancellationToken)).ToList();
        var isolatedTenants = tenants.Where(x => x.IsolationMode == TenantIsolationMode.Isolated).ToList();

        foreach (var tenant in isolatedTenants)
        {
            var shell = shellFactory.CreateShell(tenant);
            TenantShells.Add(tenant.Id, shell);
        }
    }

    public Shell GetShell(string? tenantId)
    {
        return tenantId == null ? ApplicationShell : TenantShells.TryGetValue(tenantId, out Shell? shell) ? shell : ApplicationShell;
    }
}