namespace Elsa.Workflows.Core.Services;

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