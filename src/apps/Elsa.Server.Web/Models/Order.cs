namespace Elsa.Server.Web.Models;

public class Order
{
    public string Id { get; set; }
    public string Product { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}