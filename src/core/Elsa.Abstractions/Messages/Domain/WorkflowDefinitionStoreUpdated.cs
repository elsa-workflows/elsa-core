using MediatR;

namespace Elsa.Messages.Domain
{
    /// <summary>
    /// Published when the workflow definition store is updated. 
    /// </summary>
    public class WorkflowDefinitionStoreUpdated : INotification
    {
    }
}