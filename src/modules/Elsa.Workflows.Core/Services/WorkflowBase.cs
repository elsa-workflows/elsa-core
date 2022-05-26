namespace Elsa.Workflows.Core.Services;

public abstract class WorkflowBase : IWorkflow
{
    protected virtual ValueTask BuildAsync(IWorkflowDefinitionBuilder builder, CancellationToken cancellationToken = default)
    {
        Build(builder);
        return ValueTask.CompletedTask;
    }

    protected virtual void Build(IWorkflowDefinitionBuilder builder)
    {
    }

    ValueTask IWorkflow.BuildAsync(IWorkflowDefinitionBuilder builder, CancellationToken cancellationToken) => BuildAsync(builder, cancellationToken);
}