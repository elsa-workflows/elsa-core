using Flowsharp.Models;
using Flowsharp.Persistence.Models;

namespace Flowsharp.Persistence.Specifications
{
    public interface IWorkflowDefinitionSpecificationVisitor : ISpecificationVisitor<IWorkflowDefinitionSpecificationVisitor, WorkflowDefinition>
    {
        void Visit(WorkflowStartsWithActivity specification);
    }
}