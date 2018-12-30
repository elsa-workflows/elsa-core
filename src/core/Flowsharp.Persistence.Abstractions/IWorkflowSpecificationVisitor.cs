using Flowsharp.Models;
using Flowsharp.Persistence.Specifications;

namespace Flowsharp.Persistence
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