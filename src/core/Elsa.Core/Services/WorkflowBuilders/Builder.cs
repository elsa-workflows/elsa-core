using Elsa.Services.Models;

namespace Elsa.Core.Services.WorkflowBuilders
{
    public abstract class Builder
    {
        protected Builder(WorkflowBuilder workflowBuilder)
        {
            WorkflowBuilder = workflowBuilder;
        }
        
        public WorkflowBuilder WorkflowBuilder { get; }
        
        public Workflow Build()
        {
            return WorkflowBuilder.Build();
        }
    }
}