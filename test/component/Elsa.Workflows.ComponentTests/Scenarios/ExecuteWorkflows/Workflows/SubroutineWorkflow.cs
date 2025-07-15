using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Activities.SetOutput;
using JetBrains.Annotations;

namespace Elsa.Workflows.ComponentTests.Scenarios.ExecuteWorkflows.Workflows;

[UsedImplicitly]
public class SubroutineWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString(); 
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);
        var valueInput = builder.WithInput<double>("Value");
        var output = builder.WithOutput<double>("Output");
        
        builder.Root = new Sequence
        {
            Activities =
            {
                new WriteLine(context => $"Running subroutine on value {context.GetInput<double>(valueInput)} ..., " +
                    $"correlation id is '" + context.GetWorkflowExecutionContext().CorrelationId + "'."),
                new SetOutput
                {
                    OutputName = new(output.Name),
                    OutputValue = new(context => context.GetInput<double>(valueInput) * 2)
                }
            }
        };
    }
}