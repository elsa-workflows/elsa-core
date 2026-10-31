namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class WorkflowDefinitionsReloader(IRegistriesPopulator registriesPopulator) : IWorkflowDefinitionsReloader
{
    /// <inheritdoc />
    public async Task ReloadWorkflowDefinitionsAsync(CancellationToken cancellationToken)
    {
        await registriesPopulator.PopulateAsync(cancellationToken);
    }
}