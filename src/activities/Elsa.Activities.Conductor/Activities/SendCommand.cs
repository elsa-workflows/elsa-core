using System.Threading.Tasks;
using Elsa.Activities.Conductor.Models;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Conductor
{
    [Action(
        Category = "Conductor",
        Description = "Sends a command to your application.",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class SendCommand : Activity
    {
        private readonly IEventPublisher _eventPublisher;

        public SendCommand(IEventPublisher eventPublisher)
        {
            _eventPublisher = eventPublisher;
        }
        
        [ActivityInput(
            Label = "Command",
            Hint = "The command to send.",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string CommandName { get; set; } = default!;
        
        [ActivityInput(
            UIHint = ActivityInputUIHints.MultiLine,
            Hint = "Optional data to send to your application.",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid, SyntaxNames.Json })]
        public object? Payload { get; set; } = default!;

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            await _eventPublisher.PublishAsync(new SendCommandModel(CommandName, Payload, context.WorkflowInstance.Id));
            return Done();
        }
    }
}