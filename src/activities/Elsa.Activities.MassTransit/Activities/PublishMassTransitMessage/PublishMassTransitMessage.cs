using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Services.Models;
using MassTransit;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    [Action(
        Category = "MassTransit",
        DisplayName = "Publish MassTransit Message",
        Description = "Publish an event via MassTransit."
    )]
    public class PublishMassTransitMessage : MassTransitBusActivity
    {
        public PublishMassTransitMessage(IBus bus, ConsumeContext consumeContext) : base(bus, consumeContext)
        {
        }

        [ActivityInput(Hint = "An expression that evaluates to the event to publish.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public object? Message { get; set; }

        protected override bool OnCanExecute(ActivityExecutionContext context) => Message != null;

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            await PublishEndpoint.Publish(Message!, context.CancellationToken);

            return Done();
        }
    }
}