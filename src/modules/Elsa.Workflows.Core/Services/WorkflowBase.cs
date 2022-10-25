using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Workflows.Core.Services;

/// <summary>
/// Inherit this class or implement <see cref="IWorkflow"/> directly when implementing workflows using code so that they become available to the system.
/// </summary>
public abstract class WorkflowBase : IWorkflow
{
    protected virtual ValueTask BuildAsync(IWorkflowBuilder builder, CancellationToken cancellationToken = default)
    {
        Build(builder);
        return ValueTask.CompletedTask;
    }

    protected virtual void Build(IWorkflowBuilder builder)
    {
    }

    ValueTask IWorkflow.BuildAsync(IWorkflowBuilder builder, CancellationToken cancellationToken) => BuildAsync(builder, cancellationToken);
}