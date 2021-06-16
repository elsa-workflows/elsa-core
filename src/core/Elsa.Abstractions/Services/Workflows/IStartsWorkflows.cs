using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;
using Elsa.Services.Triggers;

namespace Elsa.Services
{
    public interface IStartsWorkflows
    {
        Task<IEnumerable<RunWorkflowResult>> StartWorkflowsAsync(
            IEnumerable<TriggerFinderResult> results,
            object? input = default,
            string? contextId = default,
            CancellationToken cancellationToken = default);
    }
}