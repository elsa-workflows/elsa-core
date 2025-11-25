namespace Elsa.Workflows.IntegrationTests.Scenarios.TriggerIndexing.TestData;

/// <summary>
/// Represents a test scenario for trigger deletion with expected outcomes.
/// </summary>
public record TriggerDeletionTestScenario
{
    public required string DisplayName { get; init; }
    public required WorkflowConfig[] Workflows { get; init; }
    public required string[] ExpectedRemainingTriggerIds { get; init; }

    public override string ToString() => DisplayName;
}