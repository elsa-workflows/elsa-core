namespace Elsa.Workflows.Core.Services;

public interface IWorkflow
{
    ValueTask BuildAsync(IWorkflowBuilder builder, CancellationToken cancellationToken = default);
}