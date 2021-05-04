using MediatR;

namespace Elsa.Events
{
    /// <summary>
    /// Published when the workflow definition store is updated. 
    /// </summary>
    public class WorkflowDefinitionStoreUpdated : INotification
    {
    }
}