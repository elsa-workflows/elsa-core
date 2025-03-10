namespace Elsa.Workflows.Runtime;

/// <summary>
/// Reloads all workflows by invoking the populator.
/// </summary>
public interface IWorkflowDefinitionsReloader
{
    /// <summary>
    /// Reloads all workflows by invoking the populator.
    /// </summary>
    Task ReloadWorkflowDefinitionsAsync(CancellationToken cancellationToken = default);
}