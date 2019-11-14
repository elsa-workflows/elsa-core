using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Extensions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using Newtonsoft.Json.Linq;

namespace Elsa.Activities.MassTransit.Activities
{
    [ActivityDefinition(
        Category = "MassTransit",
        DisplayName = "Receive MassTransit Message",
        Description = "Receive a message via MassTransit."
    )]
    public class ReceiveMassTransitMessage : Activity
    {
        public static Type GetMessageType(JObject state)
        {
            var typeName = state.GetState<string>(nameof(MessageType));
            return string.IsNullOrWhiteSpace(typeName) ? null : System.Type.GetType(typeName);
        }

        [ActivityProperty(Hint = "The assembly-qualified type name of the message to receive.")]
        public Type MessageType
        {
            get => GetMessageType(State);
            set => SetState(value.AssemblyQualifiedName);
        }

        protected override bool OnCanExecute(WorkflowExecutionContext context)
        {
            var message = context.Workflow.Input[Constants.MessageInputKey];
            var messageType = MessageType;

            // TODO: Workout what needs to be done here as the message input is now a JObject,
            // and not the original message received from the bus.

            // return message != null && messageType != null && message.GetType() == messageType;
            return true;
        }

        protected override ActivityExecutionResult OnExecute(WorkflowExecutionContext context)
        {
            return Halt(true);
        }

        protected override Task<ActivityExecutionResult> OnResumeAsync(WorkflowExecutionContext context,
            CancellationToken cancellationToken)
        {
            var message = context.Workflow.Input[Constants.MessageInputKey];
            context.SetLastResult(message);

            return Task.FromResult(Done());
        }
    }
}