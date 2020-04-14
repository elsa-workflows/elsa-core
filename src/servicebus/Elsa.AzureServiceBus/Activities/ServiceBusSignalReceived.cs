using Elsa;
using Elsa.Attributes;
using Elsa.Extensions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.AzureServiceBus.Activities
{
    [ActivityDefinition(
        Category = "Azure",
        Description = "Wait for an Azure Service Bus message to be receivedd to start the activity.",
        //Icon = "fas fa-traffic-light", 
        Outcomes = new[] { OutcomeNames.Done }
     )]
    public class ServiceBusSignalReceived : Activity
    {
        public const string INPUT_VARIABLE_NAME = "AzureServiceBusMessage";

        [ActivityProperty(Hint = "The name of the queue the message is received on")]
        public string ConsumerName
        {
            get => GetState(() => "*");
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The message type that triggers this activity.")]
        public string MessageType
        {
            get => GetState<string>();
            set => SetState(value);
        }

        [ActivityProperty(
            Hint =
                "A value indicating whether the message is stored into lastResult"
        )]
        public bool ReadContent
        {
            get => GetState<bool>();
            set => SetState(value);
        }

        protected override ActivityExecutionResult OnExecute(WorkflowExecutionContext context)
        {
            return Halt(true);
        }

        protected override ActivityExecutionResult OnResume(WorkflowExecutionContext context)
        {
            if (ReadContent)
            {
                if (context.Workflow.Input.TryGetValue(INPUT_VARIABLE_NAME, out var msg) == true)
                {
                    context.CurrentScope.LastResult = Output.SetVariable(INPUT_VARIABLE_NAME, msg);
                }
            }

            return Done();
        }

        public static string GetConsumerName(JObject state)
        {
            return state.GetState<string>(nameof(ConsumerName));
        }

        public static string GetMessageType(JObject state)
        {
            return state.GetState<string>(nameof(MessageType));
        }
    }
}