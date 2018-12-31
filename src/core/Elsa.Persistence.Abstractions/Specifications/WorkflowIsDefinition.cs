using Elsa.Extensions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications
{
    public class WorkflowIsDefinition : ISpecification<Workflow, IWorkflowSpecificationVisitor>
    {
        public bool IsSatisfiedBy(Workflow item) => item.IsDefinition();

        public void Accept(IWorkflowSpecificationVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}