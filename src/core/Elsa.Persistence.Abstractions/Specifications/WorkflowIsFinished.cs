using Elsa.Extensions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications
{
    public class WorkflowIsFinished : ISpecification<Workflow, IWorkflowSpecificationVisitor>
    {
        private readonly string parentId;

        public WorkflowIsFinished(string parentId)
        {
            this.parentId = parentId;
        }
        
        public bool IsSatisfiedBy(Workflow item) => item.ParentId == parentId && item.IsFinished();

        public void Accept(IWorkflowSpecificationVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}