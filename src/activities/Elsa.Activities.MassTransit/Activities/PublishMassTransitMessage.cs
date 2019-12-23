using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Scripting;
using Elsa.Services.Models;
using MassTransit;

namespace Elsa.Activities.MassTransit.Activities
{
    [ActivityDefinition(
        Category = "MassTransit",
        DisplayName = "Publish MassTransit Message",
        Description = "Publish an event via MassTransit."
    )]
    public class PublishMassTransitMessage : MassTransitBusActivity
    {
        public PublishMassTransitMessage(IBus bus, ConsumeContext consumeContext)
            : base(bus, consumeContext)
        {
        }

        [ActivityProperty(Hint = "The assembly-qualified type name of the event to publish.")]
        public Type MessageType
        {
            get
            {
                var typeName = GetState<string>();
                return string.IsNullOrWhiteSpace(typeName) ? null : System.Type.GetType(typeName);
            }
            set => SetState(value.AssemblyQualifiedName);
        }

        [ActivityProperty(Hint = "An expression that evaluates to the event to publish.")]
        public IWorkflowExpression Message
        {
            get => GetState<IWorkflowExpression>();
            set => SetState(value);
        }

        protected override bool OnCanExecute(WorkflowExecutionContext context)
        {
            return MessageType != null;
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            var message = await context.EvaluateAsync(Message, MessageType, cancellationToken);

            await PublishEndpoint.Publish(message, cancellationToken);

            return Done();
        }
    }
}