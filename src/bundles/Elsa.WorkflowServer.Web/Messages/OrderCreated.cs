namespace Elsa.WorkflowServer.Web.Messages;

// ReSharper disable once InconsistentNaming
public interface OrderCreated
{
    string Id { get; set; }
    string CustomerId { get; set; }
    string Product { get; set; }
    int Quantity { get; set; }
    decimal Total { get; set; }
}