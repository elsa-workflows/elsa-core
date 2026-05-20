using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Handlers;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.State;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Handlers;

public class ProcessWorkflowDispatchOutboxTests
{
    private readonly IWorkflowDispatchOutboxProcessor _processor = Substitute.For<IWorkflowDispatchOutboxProcessor>();
    private readonly WorkflowDispatcherOptions _options = new()
    {
        UseTransactionalOutbox = true,
        ProcessOutboxAfterCommit = true
    };

    [Fact]
    public async Task HandleAsync_DoesNotProcess_WhenCommittedStateHasNoOutboxItems()
    {
        var handler = CreateHandler();
        var notification = CreateNotification(new WorkflowState());

        await handler.HandleAsync(notification, CancellationToken.None);

        await _processor.DidNotReceive().ProcessAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Processes_WhenCommittedStateHasOutboxItems()
    {
        var handler = CreateHandler();
        var notification = CreateNotification(new WorkflowState
        {
            Properties = new Dictionary<string, object>
            {
                [WorkflowDispatchOutboxStateExtensions.PropertyKey] = new WorkflowDispatchOutboxState
                {
                    ItemIds = ["outbox-1"]
                }
            }
        });

        await handler.HandleAsync(notification, CancellationToken.None);

        await _processor.Received(1).ProcessAsync(CancellationToken.None);
    }

    private ProcessWorkflowDispatchOutbox CreateHandler() => new(_processor, Microsoft.Extensions.Options.Options.Create(_options));

    private static WorkflowStateCommitted CreateNotification(WorkflowState workflowState)
    {
        return new(null!, workflowState, new WorkflowInstance());
    }
}
