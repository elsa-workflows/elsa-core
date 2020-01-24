using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Results;
using Elsa.Services.Models;
using MassTransit;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    [ActivityDefinition(
        Category = "MassTransit",
        DisplayName = "Publish MassTransit Message",
        Description = "Publish an event via MassTransit."
    )]
    public class PublishMassTransitMessage : MassTransitBusActivity
    {
        public PublishMassTransitMessage(IBus bus, ConsumeContext consumeContext) : base(bus, consumeContext)
        {
        }

        [ActivityProperty(Hint = "An expression that evaluates to the event to publish.")]
        public IWorkflowExpression Message
        {
            get => GetState<IWorkflowExpression>();
            set => SetState(value);
        }

        protected override bool OnCanExecute(ActivityExecutionContext context) => Message != null;

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var message = await context.EvaluateAsync(Message, cancellationToken);

            await PublishEndpoint.Publish(message, cancellationToken);

            return Done();
        }
    }
}