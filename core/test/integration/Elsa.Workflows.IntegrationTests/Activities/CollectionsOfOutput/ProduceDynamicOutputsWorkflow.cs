using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.IntegrationTests.Activities.CollectionsOfOutput;

class ProduceDynamicOutputsWorkflow : WorkflowBase
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

                // Each read goes through IActivity.GetOutput -> ActivityOutputRegister.FindOutputByActivityId(activityId, key),
                // proving the dynamic outputs are addressable by name. Fixed order also keeps the assertion deterministic.
                new WriteLine(context => Format("name", producer.GetOutput<object>(context, "name"))),
                new WriteLine(context => Format("isAdmin", producer.GetOutput<object>(context, "isAdmin"))),
                new WriteLine(context => Format("age", producer.GetOutput<object>(context, "age")))
            }
        };
    }

    private static string Format(string name, object? value) => value switch
    {
        bool boolValue => $"{name}: {boolValue} (bool)",
        string stringValue => $"{name}: {stringValue} (string)",
        double doubleValue => $"{name}: {doubleValue} (double)",
        _ => $"{name}: {value} (unexpected)"
    };
}