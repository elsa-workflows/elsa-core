using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.IntegrationTests.Activities.CollectionsOfOutput;

class ResolveDynamicOutputsByRegisterWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        var producer = new ProduceDynamicOutputs(new Dictionary<string, Output<object>>
        {
            ["name"] = new(),
            ["isAdmin"] = new(),
            ["age"] = new()
        });

        workflow.Root = new Sequence
        {
            Activities =
            {
                producer,

                // Resolve each dynamic output straight from the ActivityOutputRegister using the
                // producer's runtime activity Id + dynamic name - the same lookup ActivityOutputExpressionHandler performs.
                new WriteLine(context => Resolve(context, producer, "name")),
                new WriteLine(context => Resolve(context, producer, "isAdmin")),
                new WriteLine(context => Resolve(context, producer, "age"))
            }
        };
    }

    private static string Resolve(ExpressionExecutionContext context, IActivity producer, string name)
    {
        var register = context.GetActivityExecutionContext().WorkflowExecutionContext.GetActivityOutputRegister();
        var value = register.FindOutputByActivityId(producer.Id, name);

        return value switch
        {
            bool boolValue => $"{name}: {boolValue} (bool)",
            string stringValue => $"{name}: {stringValue} (string)",
            double doubleValue => $"{name}: {doubleValue} (double)",
            _ => $"{name}: {value} (unexpected)"
        };
    }
}
