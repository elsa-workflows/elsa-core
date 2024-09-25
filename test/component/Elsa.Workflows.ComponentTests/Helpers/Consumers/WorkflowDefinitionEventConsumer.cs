using Elsa.MassTransit.Messages;
using Elsa.Testing.Shared;
using Elsa.Testing.Shared.Services;
using Hangfire.Annotations;
using MassTransit;

namespace Elsa.Workflows.ComponentTests.Helpers;

[UsedImplicitly]
public class WorkflowDefinitionEventConsumer(WorkflowDefinitionEvents workflowDefinitionEvents) : IConsumer<WorkflowDefinitionDeleted>
{
    public Task Consume(ConsumeContext<WorkflowDefinitionDeleted> context)
    {
        workflowDefinitionEvents.OnWorkflowDefinitionDeleted(new WorkflowDefinitionDeletedEventArgs(context.Message.Id));
        return Task.CompletedTask;
    }
}