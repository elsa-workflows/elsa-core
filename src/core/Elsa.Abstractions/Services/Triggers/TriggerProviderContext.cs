using Elsa.Services.Models;

namespace Elsa.Services
{
    public class TriggerProviderContext
    {
        public TriggerProviderContext(IWorkflowBlueprintWrapper workflowWrapper, IActivityBlueprintWrapper activityWrapper)
        {
            WorkflowWrapper = workflowWrapper;
            ActivityWrapper = activityWrapper;
        }
        
        public IWorkflowBlueprintWrapper WorkflowWrapper { get; }
        public IActivityBlueprintWrapper ActivityWrapper { get; }
    }
}