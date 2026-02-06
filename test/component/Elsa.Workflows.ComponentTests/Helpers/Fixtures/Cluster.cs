using Elsa.Common.Multitenancy;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Fixtures;

[UsedImplicitly]
public class Cluster(Infrastructure infrastructure) : IAsyncDisposable
{
    public WorkflowServer Pod3 { get; set; } = new(infrastructure, "http://localhost:5003");
    public WorkflowServer Pod2 { get; set; } = new(infrastructure, "http://localhost:5002");
    public WorkflowServer Pod1 { get; set; } = new(infrastructure, "http://localhost:5001");

    public async ValueTask DisposeAsync()
    {
        // Shutdown Elsa background tasks before disposing to prevent errors from accessing disposed resources
        await ShutdownAsync(Pod3);
        await ShutdownAsync(Pod2);
        await ShutdownAsync(Pod1);
        
        await Pod3.DisposeAsync();
        await Pod2.DisposeAsync();
        await Pod1.DisposeAsync();
    }
    
    private static async Task ShutdownAsync(WorkflowServer server)
    {
        try
        {
            using var scope = server.Services.CreateScope();
            var tenantService = scope.ServiceProvider.GetService<ITenantService>();
            
            if (tenantService != null)
            {
                // Deactivating tenants will properly shutdown all recurring and background tasks
                await tenantService.DeactivateTenantsAsync();
            }
        }
        catch
        {
            // Ignore errors during shutdown - the server might already be in a bad state
        }
    }
}