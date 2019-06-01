using Elsa.Extensions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications
{
    public class WorkflowIsHalted : ISpecification<Workflow, IWorkflowSpecificationVisitor>
    {
        private readonly string parentId;

        public WorkflowIsHalted(string parentId)
        {
            this.parentId = parentId;
        }
        
        public bool IsSatisfiedBy(Workflow item) => item.ParentId == parentId && item.IsHalted();

        public void Accept(IWorkflowSpecificationVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}