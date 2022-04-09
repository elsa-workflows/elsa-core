namespace Elsa.Samples.Web1.Models;

public record Order(string Id, int Number, string CustomerId, OrderItem[] Items);
public record OrderItem(string ProductId, int Quantity);