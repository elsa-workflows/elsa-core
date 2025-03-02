using Elsa.Common.Models;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.Multitenancy;

public class MultitenancyTests(App app) : AppComponentTest(app)
{
    [Fact(Skip = "Multitenancy disabled. This test doesn't work because not all workflows are assigned the Tenant1 tenant.")]
    public async Task LoadingWorkflows_ShouldReturnWorkflows_FromCurrentTenant()
    {
        var store = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionStore>();
        var filter = new WorkflowDefinitionFilter
        {
            IsSystem = false,
            VersionOptions = VersionOptions.Latest
        };
        var workflows = await store.FindManyAsync(filter);
        Assert.All(workflows, workflow => Assert.Equal("Tenant1", workflow.TenantId));
    }
}