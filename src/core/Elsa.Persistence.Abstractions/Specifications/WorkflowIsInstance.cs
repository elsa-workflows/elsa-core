using Elsa.Extensions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications
{
    public class WorkflowIsInstance : ISpecification<Workflow, IWorkflowSpecificationVisitor>
    {
        public bool IsSatisfiedBy(Workflow item) => item.IsInstance();

        public void Accept(IWorkflowSpecificationVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}