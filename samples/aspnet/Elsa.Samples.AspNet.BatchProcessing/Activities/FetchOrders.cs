using Bogus;
using Elsa.Extensions;
using Elsa.Samples.AspNet.BatchProcessing.Models;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.Samples.AspNet.BatchProcessing.Activities;

/// <summary>
/// Fetches orders from the data source.
/// </summary>
[Activity("Demo", "Warehousing", "Fetch orders from the data source.")]
[Output(IsSerializable = false)]
public class FetchOrders : CodeActivity<IAsyncEnumerable<ICollection<Order>>>
{
    /// <summary>
    /// The total number of orders to fetch.
    /// </summary>
    [Input(
        Description = "The total number of orders to fetch.",
        DefaultValue = 1000
    )]
    public Input<int> Count { get; set; } = new(1000);

    /// <summary>
    /// The number of orders to fetch per batch.
    /// </summary>
    [Input(
        Description = "The number of orders to fetch per batch.",
        DefaultValue = 100
    )]
    public Input<int> BatchSize { get; set; } = new(100);

    /// <inheritdoc />
    protected override void Execute(ActivityExecutionContext context)
    {
        var count = Count.Get(context);
        var batchSize = BatchSize.Get(context);
        var orders = GenerateOrders(count).Chunk(batchSize).ToAsyncEnumerable();

        Result.Set(context, orders);
    }

    private IEnumerable<Order> GenerateOrders(int count)
    {
        var orderFaker = new Faker<Order>()
            .RuleFor(o => o.Id, f => Guid.NewGuid().ToString())
            .RuleFor(o => o.CustomerId, f => Guid.NewGuid().ToString())
            .RuleFor(o => o.ProductId, f => Guid.NewGuid().ToString())
            .RuleFor(o => o.Quantity, f => f.Random.Int(1, 100))
            .RuleFor(o => o.Price, f => f.Random.Decimal(0.01m, 1000.00m));

        return orderFaker.Generate(count);
    }
}