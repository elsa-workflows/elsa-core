using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IStartsWorkflows
    {
        Task<IEnumerable<RunWorkflowResult>> StartWorkflowsAsync(
            IEnumerable<TriggerFinderResult> results,
            WorkflowInput? input = default,
            string? contextId = default,
            CancellationToken cancellationToken = default);
        
        Task<IEnumerable<RunWorkflowResult>> StartWorkflowsAsync(
            IEnumerable<IWorkflowBlueprint> workflowBlueprints,
            WorkflowInput? input = default,
            string? contextId = default,
            CancellationToken cancellationToken = default);
    }
}