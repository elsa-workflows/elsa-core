using Elsa.Extensions;
using Elsa.Samples.AspNet.BatchProcessing.Activities;
using Elsa.Samples.AspNet.BatchProcessing.Models;
using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace Elsa.Samples.AspNet.BatchProcessing.Workflows;

/// <summary>
/// A workflow that processes orders in batches.
/// </summary>
public class OrderBatchProcessor : WorkflowBase
{
    /// <inheritdoc />
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Name = "Order Batch Processor - Sequence";
        var orders = builder.WithVariable<IAsyncEnumerable<ICollection<Order>>>().WithMemoryStorage();
        builder.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Fetching orders..."),
                new FetchOrders
                {
                    Result = new(orders)
                },
                new ParallelForEach<Order>
                {
                    Items = new(orders),
                    Body = new Sequence
                    {
                        Activities =
                        {
                            new WriteLine(context => $"Processing order {context.GetVariable<Order>("CurrentValue")!.Id}"),
                        }
                    }
                },
                new WriteLine("Done!")
            }
        };
    }
}