using Elsa.Models;

namespace Elsa.Persistence.Specifications
{
    public class WorkflowById : ISpecification<Workflow, IWorkflowSpecificationVisitor>
    {
        private readonly string id;

        public WorkflowById(string id)
        {
            this.id = id;
        }
        
        public bool IsSatisfiedBy(Workflow item) => item.Id == id;

        public void Accept(IWorkflowSpecificationVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}