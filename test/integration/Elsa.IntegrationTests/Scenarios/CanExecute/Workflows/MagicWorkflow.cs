using Elsa.Extensions;
using Elsa.IntegrationTests.Scenarios.CanExecute.Activities;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.IntegrationTests.Scenarios.CanExecute.Workflows;

public class MagicWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var magicNumberVariable = builder.WithVariable<int>();

        builder.Root = new Sequence
        {
            Activities =
            {
                new SetVariable<int>(magicNumberVariable, context => context.GetWorkflowInput<int>("MagicNumber")),
                new WriteLine(context => $"Magic number is {magicNumberVariable.Get(context)}"),
                new CustomActivity(context => magicNumberVariable.Get(context)),
                new WriteLine("Done")
            }
        };
    }
}