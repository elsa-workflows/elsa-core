using System.Collections.Generic;
using Elsa.Triggers;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Workflows
{
    public class RunWorkflowTrigger : ITrigger
    {
        public string ChildWorkflowInstanceId { get; set; } = default!;
    }
    
    public class RunWorkflowWorkflowTriggerProvider : WorkflowTriggerProvider<RunWorkflowTrigger, RunWorkflow>
    {
        public override IEnumerable<ITrigger> GetTriggers(TriggerProviderContext<RunWorkflow> context)
        {
            var childWorkflowInstanceId = context.GetActivity<RunWorkflow>().GetState(x => x.ChildWorkflowInstanceId);

            if (string.IsNullOrWhiteSpace(childWorkflowInstanceId))
                yield break;
            
            yield return new RunWorkflowTrigger
            {
                ChildWorkflowInstanceId = childWorkflowInstanceId!
            };
        }
    }
}