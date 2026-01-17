using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Management.UnitTests.Helpers;

/// <summary>
/// Provides helper methods for creating test data in Workflow Management unit tests.
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Creates a workflow definition with the specified parameters.
    /// </summary>
    public static WorkflowDefinition CreateWorkflowDefinition(string definitionId, string materializerName)
    {
        return new WorkflowDefinition
        {
            DefinitionId = definitionId,
            MaterializerName = materializerName,
            Version = 1
        };
    }

    /// <summary>
    /// Creates a workflow graph from a workflow, ensuring proper initialization.
    /// </summary>
    public static WorkflowGraph CreateWorkflowGraph(Workflow workflow)
    {
        // Ensure the workflow has a proper root activity with an ID
        var rootActivity = new Sequence { Id = "Root" };
        workflow.Root = rootActivity;

        // Create an ActivityNode with the root activity
        var rootNode = new ActivityNode(rootActivity, "Root");

        // Create and return the WorkflowGraph
        return new WorkflowGraph(workflow, rootNode, new[] { rootNode });
    }
}
