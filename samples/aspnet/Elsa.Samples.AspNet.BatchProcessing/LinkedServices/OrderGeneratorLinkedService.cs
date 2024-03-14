using Bogus;
using Elsa.DataSets.Abstractions;
using Elsa.Samples.AspNet.BatchProcessing.Models;

namespace Elsa.Samples.AspNet.BatchProcessing.LinkedServices;

/// <summary>
/// A linked service that generates orders.
/// </summary>
public class OrderGeneratorLinkedService : LinkedService
{
    /// <summary>
    /// Generates a number of orders.
    /// </summary>
    /// <param name="count">The number of orders to generate.</param>
    /// <returns>The generated orders.</returns>
    public IEnumerable<Order> GenerateOrders(int count)
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