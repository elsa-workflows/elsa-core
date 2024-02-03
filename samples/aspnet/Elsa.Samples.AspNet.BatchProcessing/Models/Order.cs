namespace Elsa.Samples.AspNet.BatchProcessing.Models;

public class Order
{
    public string Id { get; set; } = default!;
    public string CustomerId { get; set; } = default!;
    public string ProductId { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}