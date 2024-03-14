using Elsa.DataSets.Models;
using Elsa.Extensions;
using Elsa.Samples.AspNet.BatchProcessing.Models;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;

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
        //var orders = builder.WithVariable<IAsyncEnumerable<ICollection<Order>>>().WithMemoryStorage();
        var ordersDataSet = builder.WithVariable<DataSetReference>();
        builder.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Fetching orders..."),
                new SetVariable
                {
                    Variable = ordersDataSet,
                    Value = new(DataSetReference.Create("Orders"))
                },
                new ParallelForEach<Order>
                {
                    Items = new(ordersDataSet),
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