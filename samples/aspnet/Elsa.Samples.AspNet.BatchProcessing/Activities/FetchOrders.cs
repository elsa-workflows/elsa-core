using Elsa.Extensions;
using Elsa.Samples.AspNet.BatchProcessing.DataSets;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.Samples.AspNet.BatchProcessing.Activities;

/// <summary>
/// Fetches orders from the data source.
/// </summary>
[Activity("Demo", "Warehousing", "Fetch orders from the data source.")]
[Output(IsSerializable = false)]
public class FetchOrders : CodeActivity<OrderDataSet>
{
    /// <summary>
    /// The ID of the customer to fetch orders for.
    /// </summary>
    [Input(Description = "The ID of the customer to fetch orders for.")]
    public Input<string> CustomerId { get; set; } = default!;

    /// <inheritdoc />
    protected override void Execute(ActivityExecutionContext context)
    {
        var customerId = CustomerId.Get(context);
        var dataSet = new OrderDataSet(customerId);

        Result.Set(context, dataSet);
    }
}