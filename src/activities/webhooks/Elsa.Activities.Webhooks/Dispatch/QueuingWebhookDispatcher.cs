//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Elsa.Activities.Webhooks.Dispatch
//{
//    /// <summary>
//    /// The default strategy that process webhook execution requests by sending them to a queue.
//    /// </summary>
//    public class QueuingWebhookDispatcher : IWorkflowDefinitionDispatcher, IWorkflowInstanceDispatcher, IWorkflowDispatcher
//    {
//        private readonly ICommandSender _commandSender;
//        public QueuingWorkflowDispatcher(ICommandSender commandSender) => _commandSender = commandSender;
//        public async Task DispatchAsync(ExecuteWorkflowInstanceRequest request, CancellationToken cancellationToken = default) => await _commandSender.SendAsync(request);
//        public async Task DispatchAsync(TriggerWorkflowsRequest request, CancellationToken cancellationToken = default) => await _commandSender.SendAsync(request);
//        public async Task DispatchAsync(ExecuteWorkflowDefinitionRequest request, CancellationToken cancellationToken = default) => await _commandSender.SendAsync(request);
//    }
//}