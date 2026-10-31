using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Handlers;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.State;
using Microsoft.Extensions.Logging.Abstractions;
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
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();

    public ProcessWorkflowDispatchOutboxTests()
    {
        _serviceProvider.GetService(typeof(IWorkflowDispatchOutboxProcessor)).Returns(_processor);
    }

    [Fact]
    public async Task HandleAsync_DoesNotProcess_WhenCommittedStateHasNoOutboxItems()
    {
        var handler = CreateHandler();
        var notification = CreateNotification(new WorkflowState());

        await handler.HandleAsync(notification, CancellationToken.None);

        await _processor.DidNotReceive().TryProcessAsync(Arg.Any<CancellationToken>());
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

        await _processor.Received(1).TryProcessAsync(CancellationToken.None);
    }

    [Fact]
    public async Task HandleAsync_DoesNotThrow_WhenEagerOutboxProcessingFails()
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
        _processor.TryProcessAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException<bool>(new InvalidOperationException("Store unavailable.")));

        await handler.HandleAsync(notification, CancellationToken.None);
    }

    private ProcessWorkflowDispatchOutbox CreateHandler() => new(_serviceProvider, Microsoft.Extensions.Options.Options.Create(_options), NullLogger<ProcessWorkflowDispatchOutbox>.Instance);

    private static WorkflowStateCommitted CreateNotification(WorkflowState workflowState)
    {
        return new(null!, workflowState, new WorkflowInstance());
    }
}
