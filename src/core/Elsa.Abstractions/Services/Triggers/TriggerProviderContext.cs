using Elsa.Services.Models;

namespace Elsa.Services.Triggers
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