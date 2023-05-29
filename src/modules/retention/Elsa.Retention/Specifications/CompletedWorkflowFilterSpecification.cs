using Elsa.Models;

namespace Elsa.Retention.Specifications
{
    public class CompletedWorkflowFilterSpecification : WorkflowStatusFilterSpecification
    {
        public CompletedWorkflowFilterSpecification()
            : base(WorkflowStatus.Finished)
        {

        }
    }
}
