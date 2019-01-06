using Elsa.Extensions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications
{
    public class WorkflowIsFaulted : ISpecification<Workflow, IWorkflowSpecificationVisitor>
    {
        private readonly string parentId;

        public WorkflowIsFaulted(string parentId)
        {
            this.parentId = parentId;
        }
        
        public bool IsSatisfiedBy(Workflow item) => item.Metadata.ParentId == parentId && item.IsFaulted();

        public void Accept(IWorkflowSpecificationVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}