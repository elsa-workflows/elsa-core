using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.MassTransit.Activities
{
    [ActivityDefinition(
        Category = "MassTransit",
        DisplayName = "Receive MassTransit Message",
        Description = "Receive a message via MassTransit."
    )]
    public class ReceiveMassTransitMessage : Activity
    {
        public static Type GetMessageType(Variables state)
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

        protected override bool OnCanExecute(ActivityExecutionContext context) => context.Input.Value?.GetType() == MessageType;
        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => Suspend(true);

        protected override Task<IActivityExecutionResult> OnResumeAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var message = context.Input;
            
            return Task.FromResult<IActivityExecutionResult>(Done(message));
        }
    }
}