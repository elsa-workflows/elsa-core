using Elsa.Api.Client.RealTime.Messages;
using Elsa.Studio.Workflows.Contracts;

namespace Elsa.Studio.Workflows.Services;

/// An implementation of <see cref="IWorkflowInstanceObserver"/> that does nothing.
public class DisconnectedWorkflowInstanceObserver : IWorkflowInstanceObserver
{
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string? Name { get; set; }

    /// <inheritdoc />
    public event Func<WorkflowExecutionLogUpdatedMessage, Task> WorkflowJournalUpdated
    {
        add { }
        remove { }
    }

    /// <inheritdoc />
    public event Func<ActivityExecutionLogUpdatedMessage, Task> ActivityExecutionLogUpdated
    {
        add { }
        remove { }
    }

    /// <inheritdoc />
    public event Func<WorkflowInstanceUpdatedMessage, Task> WorkflowInstanceUpdated
    {
        add { }
        remove { }
    }
    ValueTask IAsyncDisposable.DisposeAsync() => ValueTask.CompletedTask;
}
