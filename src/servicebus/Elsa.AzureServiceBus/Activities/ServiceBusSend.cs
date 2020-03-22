using Elsa;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Elsa.AzureServiceBus.Services;

namespace Elsa.AzureServiceBus.Activities
{
    [ActivityDefinition(
        Category = "Azure",
        Description = "Send a message via azure service bus",
        RuntimeDescription = "x =>  x.state.queue ? `Send <strong></strong> to <strong>${x.state.queue}</strong>` :  x.definition.description)",
        //Icon = "fas fa-user-plus",
        Outcomes = new[] { OutcomeNames.Done })]

    public class ServiceBusSend : Activity
    {
        private readonly IServiceBusClientFactory _serviceBusProvider;

        public ServiceBusSend(IServiceBusClientFactory serviceBusProvider)
        {
            _serviceBusProvider = serviceBusProvider;
        }

        /// <summary>
        /// A list of HTTP status codes this activity can handle.
        /// </summary>

        [ActivityProperty(

            Hint = "The queue to send to"
        )]
        public string Queue
        {
            get => GetState(() => string.Empty);
            set => SetState(value);
        }

        [ActivityProperty(Hint = "Message data")]
        public WorkflowExpression<object> DataExpression
        {
            get => GetState(() => new WorkflowExpression<object>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }



        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            var data = await context.EvaluateAsync(DataExpression, cancellationToken);

            if (string.IsNullOrWhiteSpace(Queue) == false)
            {
                var stringData = data as string ?? JsonConvert.SerializeObject(data);

                var message = new Microsoft.Azure.ServiceBus.Message(Encoding.UTF8.GetBytes(stringData));



                var client = _serviceBusProvider.Create(Queue);

                await client.SendAsync(message);

                await client.CloseAsync();
            }


            return Done();

        }



    }
}
