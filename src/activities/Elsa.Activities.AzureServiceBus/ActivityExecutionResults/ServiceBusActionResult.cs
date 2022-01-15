using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Services.Models;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace Elsa.Activities.AzureServiceBus.ActivityExecutionResults
{
    public class ServiceBusActionResult : ActivityExecutionResult
    {
        public ServiceBusActionResult(ISenderClient sender, Message message)
        {
            Sender = sender;
            Message = message;
        }

        public ISenderClient Sender { get; }
        public Message Message { get; }

        protected override void Execute(ActivityExecutionContext activityExecutionContext)
        {
            async ValueTask ScheduleMessageAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken) => await Sender.SendAsync(Message);
            activityExecutionContext.WorkflowExecutionContext.RegisterTask(ScheduleMessageAsync);
        }
    }
}