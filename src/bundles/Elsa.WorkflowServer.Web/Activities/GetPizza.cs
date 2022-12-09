using Elsa.Scheduling.Activities;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Models;
using Elsa.WorkflowServer.Web.Models;

namespace Elsa.WorkflowServer.Web.Activities;

[Activity("Demo", "Models the entire process of ordering, preparing, and delivering a pizza.")]
public class GetPizza : Composite<Pizza>
{
    [Input(
        UIHint = InputUIHints.Dropdown,
        Options = new[] { "Margaritha", "Fungi", "Veggie", "Carbonara", "Pepperoni", "Hawaii" },
        DefaultValue = "Margaritha"
    )]
    public Input<string> Flavor { get; set; } = new("Margaritha");

    [Input(
        UIHint = InputUIHints.Dropdown,
        Options = new[] { 20, 30, 40, 80 }
    )]
    public Input<int> Size { get; set; } = default!;

    public GetPizza()
    {
        Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Submitting order..."),
                Delay.FromSeconds(2),
                new WriteLine("Order submitted"),
                Delay.FromSeconds(2),
                new Fork
                {
                    JoinMode = ForkJoinMode.WaitAll,
                    Branches =
                    {
                        new Sequence
                        {
                            Activities =
                            {
                                new WriteLine("Heating oven..."),
                                Delay.FromSeconds(2),
                                new WriteLine("Oven heated."),
                            }
                        },
                        new Sequence
                        {
                            Activities =
                            {
                                new WriteLine("Preparing dough and toppings..."),
                                Delay.FromSeconds(2),
                                new WriteLine("Pizza ready for heating."),
                            }
                        }
                    }
                },
                new WriteLine("Heating pizza in oven..."),
                Delay.FromSeconds(2),
                new WriteLine("Pizza is ready for delivery!"),
                Delay.FromSeconds(2),
                From(context => context.Set(Result, new Pizza(Size.Get(context), Flavor.Get(context)))),
            }
        };
    }
}