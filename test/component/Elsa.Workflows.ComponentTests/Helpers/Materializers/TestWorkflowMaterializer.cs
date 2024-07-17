using Elsa.Workflows.Activities;
using Elsa.Workflows.ComponentTests.Helpers.WorkflowProviders;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Contracts;

namespace Elsa.Workflows.ComponentTests.Helpers.Materializers;

/// A workflow materializer that deserializes workflows created from <see cref="TestWorkflowProvider"/>.
public class TestWorkflowMaterializer(IEnumerable<IWorkflowProvider> workflowProviders) : IWorkflowMaterializer
{
    /// The name of the materializer.
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