using Elsa.Activities;
using Elsa.Contracts;
using Elsa.Models;
using Elsa.Modules.Activities.Activities.Console;
using Elsa.Modules.Scheduling.Activities;
using Elsa.Runtime.Contracts;
using Elsa.Samples.Web1.Models;

namespace Elsa.Samples.Web1.Workflows;

public class OrderProcessingWorkflow : IWorkflow
{
    public void Build(IWorkflowDefinitionBuilder workflow)
    {
        var orderVariable = new Variable<Order>();

        workflow.WithRoot(new Sequence
        {
            Variables = { orderVariable },
            Activities =
            {
                new WriteLine("Creating order..."),
                new SetVariable<Order>(orderVariable, new Order("order-1", 1, "customer-1", new[] { new OrderItem("product-1", 3) })),
                Delay.FromSeconds(5),
                new WriteLine(context => $"Shipping order {orderVariable.Get(context)!.Id}."),
            }
        });
    }
}