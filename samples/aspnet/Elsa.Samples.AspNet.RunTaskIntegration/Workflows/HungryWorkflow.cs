using Elsa.Http;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Samples.AspNet.RunTaskIntegration.Workflows;

public class HungryWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var deliveredFood = builder.WithVariable<string>();

        builder.Root = new Sequence
        {
            Activities =
            {
                new HttpEndpoint
                {
                    Path = new("/hungry"),
                    SupportedMethods = new(new[] { HttpMethods.Post }),
                    CanStartWorkflow = true
                },
                new WriteLine("Hunger detected!"),
                new RunTask("OrderFood")
                {
                    Payload = new(new Dictionary<string, object> { ["Food"] = "Pizza" }),
                    Result = new Output<object>(deliveredFood)
                },
                new WriteLine(context => $"Eating the {deliveredFood.Get(context)}"),
                new WriteLine("Hunger satisfied!")
            }
        };
    }
}