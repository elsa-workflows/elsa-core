using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.TriggerIndexing.Fakes;

/// <summary>
/// A custom materializer that successfully materializes workflows from a registry.
/// </summary>
public class WorkingMaterializer(Dictionary<string, Workflow> workflowRegistry) : IWorkflowMaterializer
{
    public const string MaterializerName = "WorkingMaterializer";
    public string Name => MaterializerName;

    public ValueTask<Workflow> MaterializeAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        if (workflowRegistry.TryGetValue(definition.Id, out var workflow))
            return ValueTask.FromResult(workflow);

        throw new InvalidOperationException($"Workflow {definition.Id} not found in registry");
    }
}