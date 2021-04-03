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
    public class SendRebusMessage : Activity
    {
        private readonly ICommandSender _bus;

        public SendRebusMessage(ICommandSender bus)
        {
            _bus = bus;
        }

        [ActivityProperty(Hint = "The message to send.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public object Message { get; set; } = default!;

        [ActivityProperty(Hint = "Optional headers to send along with the message.", UIHint = ActivityPropertyUIHints.Json)]
        public IDictionary<string, string>? Headers { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            await _bus.SendAsync(Message, Headers);
            return Done();
        }
    }
}