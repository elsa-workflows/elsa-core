using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Hangfire.Annotations;

namespace Elsa.Workflows.ComponentTests.Scenarios.Multitenancy.Workflows;

[UsedImplicitly]
public class WorkflowForTenant2 : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString(); 
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);
        builder.WithTenantId("Tenant2");
        builder.Root = new WriteLine("Tenant2");
    }
}