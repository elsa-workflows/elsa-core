using Elsa.Common.Multitenancy;
using Elsa.Kafka;
using Elsa.WorkflowContexts.Abstractions;
using Elsa.Workflows;

namespace Elsa.Server.Web.WorkflowContextProviders;

public class ConsumerDefinitionWorkflowContextProvider(IConsumerDefinitionEnumerator consumerDefinitionEnumerator, ITenantAccessor tenantAccessor) : WorkflowContextProvider<ConsumerDefinition>
{
    protected override async ValueTask<ConsumerDefinition?> LoadAsync(WorkflowExecutionContext workflowExecutionContext)
    {
        var tenant = tenantAccessor.Tenant;
        var tenantId = tenant?.Id;
        var definitionId = workflowExecutionContext.Workflow.Identity.DefinitionId;

        // Load specific setting here.

        // For now, just return the first consumer definition.
        var consumerDefinitions = await consumerDefinitionEnumerator.EnumerateAsync(workflowExecutionContext.CancellationToken);
        return consumerDefinitions.FirstOrDefault();
    }
}