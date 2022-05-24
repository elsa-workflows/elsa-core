namespace Elsa.Services;

public interface IWorkflow
{
    ValueTask BuildAsync(IWorkflowDefinitionBuilder builder, CancellationToken cancellationToken = default);
}