using Elsa.Models;
using Elsa.Persistence.Specifications;

namespace Elsa.Persistence
{
    public interface IWorkflowSpecificationVisitor : ISpecificationVisitor<IWorkflowSpecificationVisitor, Workflow>
    {
        void Visit(WorkflowById specification);
        void Visit(WorkflowStartsWithActivity specification);
        void Visit(WorkflowIsBlockedOnActivity specification);
        void Visit(WorkflowIsDefinition specification);
        void Visit(WorkflowIsInstance specification);
    }
}