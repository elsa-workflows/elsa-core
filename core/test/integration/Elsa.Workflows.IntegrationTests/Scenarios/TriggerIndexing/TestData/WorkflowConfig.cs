namespace Elsa.Workflows.IntegrationTests.Scenarios.TriggerIndexing.TestData;

/// <summary>
/// Configuration for a workflow in a test scenario.
/// </summary>
public record WorkflowConfig(string Id, string DefinitionId, string MaterializerName, bool ShouldSucceed);