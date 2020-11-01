using System.Collections.Generic;
using System.Linq;
using Elsa.Triggers;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Workflows
{
    public class RunWorkflowTrigger : Trigger
    {
        public ICollection<string> ChildWorkflowInstanceIds { get; set; } = default!;
    }
    
    public class RunWorkflowTriggerProvider : TriggerProvider<RunWorkflowTrigger, RunWorkflow>
    {
        public override ITrigger GetTrigger(TriggerProviderContext<RunWorkflow> context)
        {
            var childWorkflowIds = context.GetActivity<RunWorkflow>().GetState(x => x.ChildWorkflowInstanceIds);
            
            if(!childWorkflowIds.Any())
                return NullTrigger.Instance;
            
            return new RunWorkflowTrigger
            {
                ChildWorkflowInstanceIds = childWorkflowIds
            };
        }
    }
}