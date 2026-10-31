using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.TriggerIndexing.TestData;

/// <summary>
/// Fluent builder for constructing test data with workflows and triggers.
/// </summary>
public class TriggerTestDataBuilder
{
    private readonly List<(string workflowId, string definitionId, string materializerName)> _workflows = [];

    public TriggerTestDataBuilder AddWorkflow(string workflowId, string definitionId, string materializerName)
    {
        _workflows.Add((workflowId, definitionId, materializerName));
        return this;
    }

    public (WorkflowDefinition[] Workflows, StoredTrigger[] Triggers) Build()
    {
        var workflows = _workflows
            .Select(config => CreateWorkflowDefinition(config.workflowId, config.definitionId, config.materializerName))
            .ToArray();

        var triggers = _workflows
            .Select((config, index) => CreateTrigger($"trigger{index + 1}", config.definitionId, config.workflowId, $"act{index + 1}"))
            .ToArray();

        return (workflows, triggers);
    }

    private static WorkflowDefinition CreateWorkflowDefinition(string id, string definitionId, string materializerName)
    {
        return new()
        {
            Id = id,
            DefinitionId = definitionId,
            Version = 1,
            IsPublished = true,
            IsLatest = true,
            MaterializerName = materializerName,
            StringData = "{\"root\":{\"type\":\"Elsa.Sequence\",\"id\":\"seq1\",\"version\":1,\"activities\":[]}}"
        };
    }

    private static StoredTrigger CreateTrigger(string id, string definitionId, string versionId, string activityId)
    {
        return new()
        {
            Id = id,
            WorkflowDefinitionId = definitionId,
            WorkflowDefinitionVersionId = versionId,
            Name = "TestTrigger",
            ActivityId = activityId
        };
    }
}