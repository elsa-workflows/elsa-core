using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Samples.AspNet.WorkflowSinks.Workflows;

public class OrderWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var customerVariable = builder.WithVariable<string>("CustomerName").WithWorkflowStorage();

        builder.Name = "SubmitOrder";
        builder.Root = new Sequence
        {
            Activities =
            {
                new SetVariable
                {
                    Variable = customerVariable,
                    Value = new(context => context.GetInput<string>("CustomerName"))
                },
                new WriteLine(context => $"Processing order for customer {customerVariable.Get(context)}")
            }
        };
    }
}