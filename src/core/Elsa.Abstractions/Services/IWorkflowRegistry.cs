using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowRegistry
    {
        Task<IEnumerable<WorkflowBlueprint>> GetWorkflowsAsync(CancellationToken cancellationToken = default);
        Task<WorkflowBlueprint?> GetWorkflowAsync(string id, VersionOptions version, CancellationToken cancellationToken = default);
    }
}