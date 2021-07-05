using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Activities.Signaling.Services
{
    public interface ISignaler
    {
        /// <summary>
        /// Runs all workflows that start with or are blocked on the <see cref="SignalReceived"/> activity.
        /// </summary>
        Task<IEnumerable<CollectedWorkflow>> TriggerSignalTokenAsync(string signalToken, object? input = default, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Runs all workflows that start with or are blocked on the <see cref="SignalReceived"/> activity.
        /// </summary>
        Task<IEnumerable<CollectedWorkflow>> TriggerSignalAsync(string signal, object? input = null, string? workflowInstanceId = null, string? correlationId = null, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Dispatches all workflows that start with or are blocked on the <see cref="SignalReceived"/> activity.
        /// </summary>
        Task<IEnumerable<CollectedWorkflow>> DispatchSignalTokenAsync(string token, object? input = default, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Dispatches all workflows that start with or are blocked on the <see cref="SignalReceived"/> activity.
        /// </summary>
        Task<IEnumerable<CollectedWorkflow>> DispatchSignalAsync(string signal, object? input = default, string? workflowInstanceId = default, string? correlationId = default, CancellationToken cancellationToken = default);
    }
}