using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Rebus
{
    [Action(Category = "Rebus", Description = "Publishes a message.", Outcomes = new[] { OutcomeNames.Done })]
    public class PublishRebusMessage : Activity
    {
        private readonly IEventPublisher _eventPublisher;

        public PublishRebusMessage(IEventPublisher eventPublisher)
        {
            _eventPublisher = eventPublisher;
        }

        [ActivityInput(Hint = "The message to publish.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public object Message { get; set; } = default!;

        [ActivityInput(Hint = "Optional headers to send along with the message.", UIHint = ActivityInputUIHints.Json)]
        public IDictionary<string, string>? Headers { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            await _eventPublisher.PublishAsync(Message, Headers);
            return Done();
        }
    }
}