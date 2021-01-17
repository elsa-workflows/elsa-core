using Elsa.Services.Models;
using MediatR;

namespace Elsa.Events
{
    public class ScopeEvicted : INotification
    {
        public ScopeEvicted(WorkflowExecutionContext workflowExecutionContext, IActivityBlueprint evictedScope)
        {
            WorkflowExecutionContext = workflowExecutionContext;
            EvictedScope = evictedScope;
        }
        
        public WorkflowExecutionContext WorkflowExecutionContext { get; }
        public IActivityBlueprint EvictedScope { get; }
    }
}