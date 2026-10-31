using Elsa.Workflows.Management.Activities.SetOutput;
using JetBrains.Annotations;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Composition.DispatchWorkflows.Workflows;

/// <summary>A child that sets one workflow output and finishes.</summary>
[UsedImplicitly]
public class ChildWorkflowWithOutput : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();
    public const string OutputName = "Parcel";

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);
        builder.Root = new SetOutput
        {
            OutputName = new(OutputName),
            OutputValue = new("P-1")
        };
    }
}
