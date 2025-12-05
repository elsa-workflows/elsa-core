using Elsa.Common.Multitenancy;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.WorkflowActivities;

public class MultitenancyActivityProviderTests(App app) : AppComponentTest(app)
{
    [Fact(DisplayName = "Workflow activities should be isolated by tenant")]
    public async Task WorkflowActivities_ShouldBeIsolatedByTenant()
    {
        // Arrange: Create workflow for Tenant1
        var tenant1Workflow = await CreateWorkflowForTenant("Tenant1Workflow", "Tenant1");
        
        // Arrange: Create workflow for Tenant2
        var tenant2Workflow = await CreateWorkflowForTenant("Tenant2Workflow", "Tenant2");
        
        // Act: Get activity descriptors for Tenant1
        var tenant1Descriptors = await GetDescriptorsForTenant("Tenant1");
        
        // Act: Get activity descriptors for Tenant2
        var tenant2Descriptors = await GetDescriptorsForTenant("Tenant2");
        
        // Assert: Tenant1 should only see their own workflow
        var tenant1WorkflowActivity = tenant1Descriptors.FirstOrDefault(d => d.Name == "Tenant1Workflow");
        var tenant2WorkflowInTenant1 = tenant1Descriptors.FirstOrDefault(d => d.Name == "Tenant2Workflow");
        
        Assert.NotNull(tenant1WorkflowActivity);
        Assert.Null(tenant2WorkflowInTenant1); // Should NOT see Tenant2's workflow
        
        // Assert: Tenant2 should only see their own workflow
        var tenant2WorkflowActivity = tenant2Descriptors.FirstOrDefault(d => d.Name == "Tenant2Workflow");
        var tenant1WorkflowInTenant2 = tenant2Descriptors.FirstOrDefault(d => d.Name == "Tenant1Workflow");
        
        Assert.NotNull(tenant2WorkflowActivity);
        Assert.Null(tenant1WorkflowInTenant2); // Should NOT see Tenant1's workflow
    }
    
    private async Task<string> CreateWorkflowForTenant(string workflowName, string tenantId)
    {
        using var scope = WorkflowServer.Services.CreateScope();
        var tenantAccessor = scope.ServiceProvider.GetRequiredService<ITenantAccessor>();
        
        // Set tenant context
        using var tenantScope = tenantAccessor.PushContext(new Tenant { Id = tenantId });
        
        var importer = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionImporter>();
        var request = new SaveWorkflowDefinitionRequest
        {
            Model = new WorkflowDefinitionModel
            {
                Name = workflowName,
                DefinitionId = workflowName,
                Options = new WorkflowOptions
                {
                    UsableAsActivity = true
                }
            },
            Publish = true
        };
        
        var result = await importer.ImportAsync(request);
        return result.WorkflowDefinition.Id;
    }
    
    private async Task<IEnumerable<ActivityDescriptor>> GetDescriptorsForTenant(string tenantId)
    {
        using var scope = WorkflowServer.Services.CreateScope();
        var tenantAccessor = scope.ServiceProvider.GetRequiredService<ITenantAccessor>();
        
        // Set tenant context
        using var tenantScope = tenantAccessor.PushContext(new Tenant { Id = tenantId });
        
        var provider = scope.ServiceProvider.GetRequiredService<WorkflowDefinitionActivityProvider>();
        return await provider.GetDescriptorsAsync();
    }
}
