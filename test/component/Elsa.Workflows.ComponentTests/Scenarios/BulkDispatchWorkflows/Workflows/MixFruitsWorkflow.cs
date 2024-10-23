using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;

namespace Elsa.Workflows.ComponentTests.Scenarios.BulkDispatchWorkflows.Workflows;

public class MixFruitsWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder builder)
    {
        var fruits = new[]
        {
            "Apple", "Banana", "Cherry"
        };
        
        builder.WithDefinitionId(DefinitionId);
        builder.Root = new Sequence
        {
            Activities =
            {
                new Runtime.Activities.BulkDispatchWorkflows
                {
                    WorkflowDefinitionId = new(FruitWorkflow.DefinitionId),
                    Items = new(fruits),
                    WaitForCompletion = new(true)
                }
            }
        };
    }
}