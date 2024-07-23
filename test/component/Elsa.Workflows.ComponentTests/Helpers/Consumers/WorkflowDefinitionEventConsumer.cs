using Elsa.MassTransit.Messages;
using Elsa.Testing.Shared;
using Hangfire.Annotations;
using MassTransit;

namespace Elsa.Workflows.ComponentTests.Helpers.Consumers;

[UsedImplicitly]
public class WorkflowDefinitionEventConsumer(IWorkflowDefinitionEvents workflowDefinitionEvents) : IConsumer<WorkflowDefinitionDeleted>
{
    public Task Consume(ConsumeContext<WorkflowDefinitionDeleted> context)
    {
        workflowDefinitionEvents.OnWorkflowDefinitionDeleted(new WorkflowDefinitionDeletedEventArgs(context.Message.Id));
        return Task.CompletedTask;
    }
}