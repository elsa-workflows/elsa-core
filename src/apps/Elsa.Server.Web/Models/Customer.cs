namespace Elsa.Server.Web.Models;

public class Customer
{
    public string Id { get; set; }
    public string Name { get; set; }
    public ICollection<Order> Orders { get; set; } = [];
}