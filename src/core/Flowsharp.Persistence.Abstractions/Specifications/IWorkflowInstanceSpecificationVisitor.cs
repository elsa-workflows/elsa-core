using Flowsharp.Persistence.Models;

namespace Flowsharp.Persistence.Specifications
{
    public interface IWorkflowInstanceSpecificationVisitor : ISpecificationVisitor<IWorkflowInstanceSpecificationVisitor, WorkflowInstance>
    {
        void Visit(WorkflowIsBlockedOnActivity specification);
    }
}