using Bogus;
using Elsa.Extensions;
using Elsa.Samples.AspNet.BatchProcessing.Models;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Signals;

namespace Elsa.Samples.AspNet.BatchProcessing.Activities;

[Activity("Demo", "Warehousing", "Fetch products from the data source.")]
[Output(IsSerializable = false)]
public class FetchProducts : CodeActivity<ICollection<Product>>
{
    private const string CurrentBathKey = nameof(CurrentBathKey);

    /// <summary>
    /// The total number of products to fetch.
    /// </summary>
    [Input(Description = "The total number of products to fetch.")]
    public Input<int> Count { get; set; } = new(100);

    /// <summary>
    /// The number of products to fetch per batch.
    /// </summary>
    [Input(Description = "The number of products to fetch per batch.")]
    public Input<int> BatchSize { get; set; } = new(100);

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var count = Count.Get(context);
        var batchSize = BatchSize.Get(context);
        var currentBatch = context.ActivityInput.TryGetValue(CurrentBathKey, out var currentBatchValue) ? (int)currentBatchValue : 0;
        var orders = GenerateProducts(count).Skip(currentBatch * batchSize).Take(batchSize).ToList();

        Result.Set(context, orders);
        await context.CompleteActivityAsync();
        
        if (orders.Any())
        {
            currentBatch++;
            context.SetProperty(CurrentBathKey, currentBatch);
            
            // Schedule the next batch.
            await context.SendSignalAsync(new ScheduleChildActivity(this, new Dictionary<string, object> { [CurrentBathKey] = currentBatch }));
        }
    }

    private IEnumerable<Product> GenerateProducts(int count)
    {
        var productFaker = new Faker<Product>()
            .RuleFor(o => o.Id, f => Guid.NewGuid().ToString())
            .RuleFor(o => o.Name, f => Guid.NewGuid().ToString())
            .RuleFor(o => o.Price, f => f.Random.Decimal(0.01m, 1000.00m));

        return productFaker.Generate(count);
    }
}