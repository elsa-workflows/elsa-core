using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Services
{
    public interface IWorkflowDefinitionManager
    {
        Task<IEnumerable<WorkflowDefinition>> ListAsync(
            VersionOptions version,
            CancellationToken cancellationToken);
    }
}