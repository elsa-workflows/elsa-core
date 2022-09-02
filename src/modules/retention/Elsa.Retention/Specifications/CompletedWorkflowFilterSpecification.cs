using Elsa.Models;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Retention.Specifications
{
    public class CompletedWorkflowFilterSpecification : WorkflowStatusFilterSpecification
    {
        public CompletedWorkflowFilterSpecification()
            : base(new[] { WorkflowStatus.Running, WorkflowStatus.Finished })
        {

        }
    }
}
