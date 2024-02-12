using Elsa.Samples.AspNet.BatchProcessing.Activities;
using Elsa.Samples.AspNet.BatchProcessing.Models;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;

namespace Elsa.Samples.AspNet.BatchProcessing.Workflows;

public class OrderBatchProcessor : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var orders = builder.WithVariable<IAsyncEnumerable<Order>>();
        builder.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Fetching orders..."),
                new FetchOrders(),
                new ParallelForEach<Order>
                {
                    Items = new(orders) 
                },
                new WriteLine("Done!")
            }
        };
    }
}