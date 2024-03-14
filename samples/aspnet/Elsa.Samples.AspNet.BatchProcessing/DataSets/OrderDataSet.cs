using Elsa.DataSets.Abstractions;

namespace Elsa.Samples.AspNet.BatchProcessing.DataSets;

public sealed class OrderDataSet : DataSet
{
    public OrderDataSet()
    {
    }

    public OrderDataSet(string customerId)
    {
        CustomerId = customerId;
    }

    public string CustomerId { get; set; }
}