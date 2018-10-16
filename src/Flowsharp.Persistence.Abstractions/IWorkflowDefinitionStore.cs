using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;
using Flowsharp.Persistence.Models;
using Flowsharp.Persistence.Specifications;

namespace Flowsharp.Persistence
{
    public interface IWorkflowDefinitionStore
    {
        Task<IEnumerable<WorkflowDefinition>> GetManyAsync(ISpecification<WorkflowDefinition, IWorkflowDefinitionSpecificationVisitor> specification, CancellationToken cancellationToken);
        Task AddAsync(WorkflowDefinition value, CancellationToken cancellationToken);
    }
}