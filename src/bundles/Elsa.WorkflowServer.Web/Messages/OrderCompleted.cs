namespace Elsa.WorkflowServer.Web.Messages;

// ReSharper disable once InconsistentNaming
public interface OrderCompleted
{
    string OrderId { get; set; }
}