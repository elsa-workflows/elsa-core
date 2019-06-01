using Elsa.Models;

namespace Elsa.Builders
{
    public abstract class BuilderNode
    {
        protected BuilderNode(WorkflowBuilder workflowBuilder, IActivity activity)
        {
            WorkflowBuilder = workflowBuilder;
            Activity = activity;
        }
        
        public IActivity Activity { get; }
        protected WorkflowBuilder WorkflowBuilder { get; set; }

        public abstract void ApplyTo(Workflow workflow);
        public Workflow BuildWorkflow() => WorkflowBuilder.BuildWorkflow();
    }
}