using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
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

        [ActivityProperty(Hint = "The message to publish.")]
        public object Message { get; set; } = default!;

        [ActivityProperty(Hint = "Optional headers to send along with the message.")]
        public IDictionary<string, string>? Headers { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            await _eventPublisher.PublishAsync(Message, Headers);
            return Done();
        }
    }
}