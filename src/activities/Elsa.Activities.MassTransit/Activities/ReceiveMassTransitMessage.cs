using System;
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
            var messageTypeName = context.Workflow.Input.GetVariable(Constants.MessageTypeNameInputKey);
            var messageInputType = System.Type.GetType(messageTypeName.ToString());
            var messageType = MessageType;
            
            return messageInputType != null && messageType != null && messageInputType == messageType;
        }

        protected override ActivityExecutionResult OnExecute(WorkflowExecutionContext context)
        {
            return Halt(true);
        }

        protected override ActivityExecutionResult OnResume(WorkflowExecutionContext context)
        {
            var message = context.Workflow.Input.GetVariable(Constants.MessageInputKey);
            context.SetLastResult(message);

            return Done();
        }
    }
}