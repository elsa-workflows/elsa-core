using Flowsharp.Models;

namespace Flowsharp.Persistence.Specifications
{
    public class WorkflowIsInstance : ISpecification<Workflow, IWorkflowSpecificationVisitor>
    {
        public bool IsSatisfiedBy(Workflow item) => item.Metadata.ParentId != null;

        public void Accept(IWorkflowSpecificationVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}