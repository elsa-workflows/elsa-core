using Elsa.DataSets.Abstractions;
using Elsa.Samples.AspNet.BatchProcessing.LinkedServices;
using Elsa.Samples.AspNet.BatchProcessing.Models;

namespace Elsa.Samples.AspNet.BatchProcessing.DataSets;

public sealed class OrderDataSet : DataSet<Order, OrderGeneratorLinkedService>
{
    public OrderDataSet()
    {
    }

    public OrderDataSet(string customerId)
    {
        CustomerId = customerId;
    }

    public string CustomerId { get; set; }
    
    protected override IAsyncEnumerable<Order> ReadAsync(OrderGeneratorLinkedService linkedService, CancellationToken cancellationToken = default)
    {
        var orders = linkedService.GenerateOrders(100).ToAsyncEnumerable();
        return orders;
    }
}