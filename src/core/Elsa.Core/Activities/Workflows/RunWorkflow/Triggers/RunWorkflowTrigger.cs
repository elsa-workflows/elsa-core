using Elsa.Triggers;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Workflows
{
    public class RunWorkflowTrigger : Trigger
    {
        public string ChildWorkflowInstanceId { get; set; } = default!;
    }
    
    public class RunWorkflowTriggerProvider : TriggerProvider<RunWorkflowTrigger, RunWorkflow>
    {
        public override ITrigger GetTrigger(TriggerProviderContext<RunWorkflow> context)
        {
            var childWorkflowInstanceId = context.GetActivity<RunWorkflow>().GetState(x => x.ChildWorkflowInstanceId);
            
            if(string.IsNullOrWhiteSpace(childWorkflowInstanceId))
                return NullTrigger.Instance;
            
            return new RunWorkflowTrigger
            {
                ChildWorkflowInstanceId = childWorkflowInstanceId!
            };
        }
    }
}