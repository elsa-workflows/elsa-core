using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Web.Persistence.Abstractions.Models;

namespace Flowsharp.Web.Persistence.Abstractions.Services
{
    public interface IWorkflowDefinitionStore
    {
        Task<IEnumerable<WorkflowDefinition>> ListAsync(CancellationToken cancellationToken);
        Task<WorkflowDefinition> GetAsync(string id, CancellationToken cancellationToken);
    }
}