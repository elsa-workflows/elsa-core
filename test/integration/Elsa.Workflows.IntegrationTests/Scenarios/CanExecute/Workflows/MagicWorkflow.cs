using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.IntegrationTests.Scenarios.CanExecute.Activities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.CanExecute.Workflows;

public class MagicWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var magicNumberVariable = builder.WithVariable<int>();

        builder.Root = new Sequence
        {
            Activities =
            {
                new SetVariable<int>(magicNumberVariable, context => context.GetInput<int>("MagicNumber")),
                new WriteLine(context => $"Magic number is {magicNumberVariable.Get(context)}"),
                new CustomActivity(context => magicNumberVariable.Get(context)),
                new WriteLine("Done")
            }
        };
    }
}