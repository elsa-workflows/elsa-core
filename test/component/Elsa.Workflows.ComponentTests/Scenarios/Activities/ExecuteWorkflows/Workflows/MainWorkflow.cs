using System.Text.Json;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.ExecuteWorkflows.Workflows;

public class MainWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);
        var workflowResult = builder.WithVariable<ExecuteWorkflowResult>();
        builder.Root = new Sequence
        {
            Activities =
            {
                new ExecuteWorkflow
                {
                    WorkflowDefinitionId = new(SubroutineWorkflow.DefinitionId),
                    Input = new(new Dictionary<string, object>
                    {
                        ["Value"] = 21
                    }),
                    Result = new(workflowResult)
                },
                new WriteLine(context => $"Subroutine output: {JsonSerializer.Serialize(workflowResult.Get(context))}")
            }
        };
    }
}