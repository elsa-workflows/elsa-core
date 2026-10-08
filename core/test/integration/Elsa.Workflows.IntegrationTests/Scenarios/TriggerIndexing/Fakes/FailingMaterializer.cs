using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.TriggerIndexing.Fakes;

/// <summary>
/// A custom materializer that throws an exception when materialization is attempted.
/// This simulates the "Provider not found" scenario.
/// </summary>
public class FailingMaterializer : IWorkflowMaterializer
{
    public const string MaterializerName = "FailingMaterializer";
    public string Name => MaterializerName;

    public ValueTask<Workflow> MaterializeAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default) =>
        ValueTask.FromException<Workflow>(new("Provider not found"));
}