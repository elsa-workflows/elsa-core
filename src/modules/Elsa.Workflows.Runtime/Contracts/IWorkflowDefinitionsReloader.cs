namespace Elsa.Workflows.Runtime.Contracts;

/// Reloads all workflows by invoking the populator.
public interface IWorkflowDefinitionsReloader
{
    /// Reloads all workflows by invoking the populator.
    Task ReloadWorkflowDefinitionsAsync(CancellationToken cancellationToken = default);
}