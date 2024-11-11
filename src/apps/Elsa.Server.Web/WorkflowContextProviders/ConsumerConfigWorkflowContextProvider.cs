using Elsa.Kafka;
using Elsa.WorkflowContexts.Abstractions;
using Elsa.Workflows;

namespace Elsa.Server.Web.WorkflowContextProviders;

public class ConsumerDefinitionWorkflowContextProvider(IConsumerDefinitionEnumerator consumerDefinitionEnumerator) : WorkflowContextProvider<ConsumerDefinition>
{
    protected override async ValueTask<ConsumerDefinition?> LoadAsync(WorkflowExecutionContext workflowExecutionContext)
    {
        var consumerDefinitions = await consumerDefinitionEnumerator.EnumerateAsync(workflowExecutionContext.CancellationToken);
        return consumerDefinitions.FirstOrDefault();
    }
}