using Elsa.Scheduling.Activities;
using Elsa.Samples.Web1.Models;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Samples.Web1.Workflows;

public class OrderProcessingWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
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