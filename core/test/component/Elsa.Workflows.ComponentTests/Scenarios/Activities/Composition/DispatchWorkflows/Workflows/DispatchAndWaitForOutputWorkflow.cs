using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Composition.DispatchWorkflows.Workflows;

/// <summary>Waits for a <see cref="ChildWorkflowWithOutput"/> and writes the keys of the <see cref="DispatchWorkflow"/> result.</summary>
public class DispatchAndWaitForOutputWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();
    public const string ResultKeysPrefix = "Result keys: ";

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);
        var result = builder.WithVariable<object>();

        builder.Root = new Sequence
        {
            Activities =
            {
                new DispatchWorkflow
                {
                    WorkflowDefinitionId = new(ChildWorkflowWithOutput.DefinitionId),
                    WaitForCompletion = new(true),
                    Result = new(result)
                },
                new WriteLine(context => ResultKeysPrefix + string.Join(",", ((IDictionary<string, object>)result.Get(context)!).Keys.Order()))
            }
        };
    }
}
