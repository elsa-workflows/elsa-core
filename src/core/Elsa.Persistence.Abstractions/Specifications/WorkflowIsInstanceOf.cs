using Elsa.Models;

namespace Elsa.Persistence.Specifications
{
    public class WorkflowIsInstanceOf : ISpecification<Workflow, IWorkflowSpecificationVisitor>
    {
        private readonly string parentId;

        public WorkflowIsInstanceOf(string parentId)
        {
            this.parentId = parentId;
        }
        
        public bool IsSatisfiedBy(Workflow item) => item.Metadata.ParentId == parentId;

        public void Accept(IWorkflowSpecificationVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}