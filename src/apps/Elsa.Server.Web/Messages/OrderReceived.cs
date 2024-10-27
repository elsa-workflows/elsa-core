namespace Elsa.Server.Web.Messages;

public class OrderReceived
{
    public string OrderId { get; set; } = default!;
    public decimal OrderTotal { get; set; } = default!;
}