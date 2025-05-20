using Elsa.Workflows.Activities;
using Elsa.Workflows.ComponentTests.WorkflowProviders;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime;

namespace Elsa.Workflows.ComponentTests.Materializers;

/// <summary>
/// A workflow materializer that deserializes workflows created from <see cref="TestWorkflowProvider"/>.
/// </summary>
public class TestWorkflowMaterializer(IEnumerable<IWorkflowsProvider> workflowProviders) : IWorkflowMaterializer
{
    /// <summary>
    /// The name of the materializer.
    /// </summary>
    public const string MaterializerName = "Test";

    /// <inheritdoc />
    public string Name => MaterializerName;

    /// <inheritdoc />
    public ValueTask<Workflow> MaterializeAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        var testProvider = (TestWorkflowProvider)workflowProviders.Single(x => x is TestWorkflowProvider);
        var materializedWorkflow = testProvider.MaterializedWorkflows.First(x => x.Workflow.Identity.Id == definition.Id);

        return ValueTask.FromResult(materializedWorkflow.Workflow);
    }
}