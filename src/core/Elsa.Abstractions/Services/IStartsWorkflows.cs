using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Triggers;

namespace Elsa.Services
{
    public interface IStartsWorkflows
    {
        Task StartWorkflowsAsync(
            IEnumerable<TriggerFinderResult> results,
            object? input = default,
            string? contextId = default,
            CancellationToken cancellationToken = default);
    }
}