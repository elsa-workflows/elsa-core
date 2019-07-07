using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Core.Services;
using Elsa.Extensions;
using Elsa.Results;
using Elsa.Services.Models;
using Newtonsoft.Json.Linq;

namespace Elsa.Activities.MassTransit.Activities
{
    public class ReceiveMassTransitMessage : Activity
    {
        public static Type GetMessageType(JObject state)
        {
            var typeName = state.GetState<string>(nameof(MessageType));
            return string.IsNullOrWhiteSpace(typeName) ? null : Type.GetType(typeName);
        }
        
        public Type MessageType
        {
            get => GetMessageType(State);
            set => SetState(value.AssemblyQualifiedName);
        }

        protected override bool OnCanExecute(WorkflowExecutionContext context)
        {
            var message = context.Workflow.Input["message"];
            var messageType = MessageType;
            
            return message != null && messageType != null && message.GetType() == messageType;
        }

        protected override ActivityExecutionResult OnExecute(WorkflowExecutionContext context)
        {
            return Halt(true);
        }

        protected override Task<ActivityExecutionResult> OnResumeAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            var message = context.Workflow.Input["message"];
            context.SetLastResult(message);

            return Task.FromResult(Done());
        }
    }
}