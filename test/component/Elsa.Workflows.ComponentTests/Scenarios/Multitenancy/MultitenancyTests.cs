using Elsa.Common.Models;
using Elsa.Workflows.ComponentTests.Helpers.Abstractions;
using Elsa.Workflows.ComponentTests.Helpers.Fixtures;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.Multitenancy;

public class MultitenancyTests(App app) : AppComponentTest(app)
{
    [Fact]
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