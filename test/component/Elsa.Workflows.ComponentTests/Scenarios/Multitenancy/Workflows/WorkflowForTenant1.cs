using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Hangfire.Annotations;

namespace Elsa.Workflows.ComponentTests.Scenarios.Multitenancy.Workflows;

[UsedImplicitly]
public class WorkflowForTenant1 : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString(); 
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);
        builder.WithTenantId("Tenant1");
        builder.Root = new WriteLine("Tenant1");
    }
}