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
using Elsa.Extensions;
using Elsa.Persistence;
using Newtonsoft.Json.Linq;
using Elsa.AzureServiceBus.Models;
using Elsa.AzureServiceBus.Services;

namespace Elsa.AzureServiceBus.Activities
{
    [ActivityDefinition(
        Category = "Azure",
        Description = "Send a message and halt workflow execution until the specified signal is received.",
         RuntimeDescription = "x => !!x.state.queue ? `<strong>${x.state.dataExpression.expression}</strong> -> <strong>${x.state.queue}</strong><br/>${x.state.dataExpression.syntax}` : x.definition.description",
        Icon = "fas fa-traffic-light",
        Outcomes = "x => !!x.state.signals ? x.state.signals : []")]

    public class ServiceBusSignaled : Activity
    {
        public const string INPUT_VARIABLE_NAME = "AzureServiceBus_Signal";

        private readonly IWorkflowExpressionEvaluator _expressionEvaluator;
        private readonly IServiceBusClientFactory _serviceBusProvider;
        private readonly ITokenService _tokenService;

        public ServiceBusSignaled(IWorkflowExpressionEvaluator expressionEvaluator, IServiceBusClientFactory serviceBusProvider, ITokenService tokenService)
        {

            _serviceBusProvider = serviceBusProvider;
            _expressionEvaluator = expressionEvaluator;
            _tokenService = tokenService;
        }

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

        [ActivityProperty(
            Type = ActivityPropertyTypes.List,
            Hint = "An expression that evaluates to the name of the signal to wait for."
        )]
        public HashSet<string> Signals
        {
            get => GetState(() => new HashSet<string> { "Approved" });
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The name of the queue to receive the message on")]
        public string ConsumerName
        {
            get => GetState(() => "*");
            set => SetState(value);
        }



        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            var data = await context.EvaluateAsync(DataExpression, cancellationToken);

            if (string.IsNullOrWhiteSpace(Queue) == false)
            {
                //we need to generate a signal for each of the signals
                var actions = new Dictionary<string, string>();

                var workflowInstanceId = context.Workflow.Id;

                foreach (var signal in Signals)
                {
                    //generate an action token foreach
                    actions.Add(signal, _tokenService.CreateToken(new Signal(signal, workflowInstanceId)));
                }



                var jsonData = JsonConvert.SerializeObject(new ServiceBusSignalMessage<object>
                {
                    Actions = actions,
                    Data = data
                });

                var message = new Microsoft.Azure.ServiceBus.Message(Encoding.UTF8.GetBytes(jsonData))
                {
                    ContentType = "application/json"

                };



                var client = _serviceBusProvider.Create(Queue);

                await client.SendAsync(message);

                await client.CloseAsync();
            }

            //wait for one of the signals to be triggered
            return Halt(false);
        }



        protected override Task<bool> OnCanExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {

            bool result = false;

            //check to see if any of the signals have been triggered
            var signalVar = context.Workflow.Input.GetVariable<string>(INPUT_VARIABLE_NAME);

            foreach (var s in Signals)
            {
                if (s == signalVar)
                {
                    result = true;
                    break;
                }
            }

            return Task.FromResult(result);



        }


        protected async override Task<ActivityExecutionResult> OnResumeAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {

            return await Task.FromResult(Outcome(context.Workflow.Input.GetVariable<string>(INPUT_VARIABLE_NAME)));
        }




        public static string GetConsumerName(JObject state)
        {
            return state.GetState<string>(nameof(ConsumerName));
        }

    }
}
