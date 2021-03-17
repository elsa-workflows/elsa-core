using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Client.Models;
using Elsa.Activities.Telnyx.Client.Services;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Exceptions;
using Elsa.Services;
using Elsa.Services.Models;
using Refit;

namespace Elsa.Activities.Telnyx.Activities
{
    [Action(
        Category = Constants.Category,
        Description = "Hang up the call.",
        Outcomes = new[] { OutcomeNames.Done },
        DisplayName = "Hangup Call"
    )]
    public class HangupCall : Activity
    {
        private readonly ITelnyxClient _telnyxClient;

        public HangupCall(ITelnyxClient telnyxClient)
        {
            _telnyxClient = telnyxClient;
        }

        [ActivityProperty(Label = "Call Control ID", Hint = "Unique identifier and token for controlling the call")]
        public string CallControlId { get; set; } = default!;
        
        [ActivityProperty(Label = "Client State", Hint = "Use this field to add state to every subsequent webhook. It must be a valid Base-64 encoded string.")]
        public string? ClientState { get; set; }

        [ActivityProperty(Label = "Command ID", Hint = "Use this field to avoid duplicate commands. Telnyx will ignore commands with the same Command ID.")]
        public string? CommandId { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            await HangupCallAsync(context.CancellationToken);
            return Done();
        }

        private async ValueTask HangupCallAsync(CancellationToken cancellationToken)
        {
            var request = new HangupCallRequest(ClientState, CommandId);
            
            try
            {
                await _telnyxClient.Calls.HangupCallAsync(CallControlId, request, cancellationToken);
            }
            catch (ApiException e)
            {
                throw new WorkflowException(e.Content ?? e.Message, e);
            }
        }
    }
}