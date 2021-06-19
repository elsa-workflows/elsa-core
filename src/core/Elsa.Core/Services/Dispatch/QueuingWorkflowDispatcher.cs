using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Services.Dispatch
{
    /// <summary>
    /// The default strategy that process workflow execution requests by sending them to a queue.
    /// </summary>
    public class QueuingWorkflowDispatcher : IWorkflowDefinitionDispatcher, IWorkflowInstanceDispatcher, IWorkflowDispatcher
    {
        private readonly ICommandSender _commandSender;
        public QueuingWorkflowDispatcher(ICommandSender commandSender) => _commandSender = commandSender;
        public async Task DispatchAsync(ExecuteWorkflowInstanceRequest request, CancellationToken cancellationToken = default) => await _commandSender.SendAsync(request);
        public async Task DispatchAsync(TriggerWorkflowsRequest request, CancellationToken cancellationToken = default) => await _commandSender.SendAsync(request);
        public async Task DispatchAsync(ExecuteWorkflowDefinitionRequest request, CancellationToken cancellationToken = default) => await _commandSender.SendAsync(request);
    }
}