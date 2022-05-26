namespace Elsa.Workflows.Core.Services;

public interface IWorkflow
{
    ValueTask BuildAsync(IWorkflowDefinitionBuilder builder, CancellationToken cancellationToken = default);
}