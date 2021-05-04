using System;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Client.Models;
using Elsa.Activities.Telnyx.Client.Services;
using Elsa.Activities.Telnyx.Extensions;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Builders;
using Elsa.Design;
using Elsa.Exceptions;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using Refit;

namespace Elsa.Activities.Telnyx.Activities
{
    [Action(
        Category = Constants.Category,
        Description = "Bridge two call control calls.",
        Outcomes = new[] { OutcomeNames.Done, TelnyxOutcomeNames.CallIsNoLongerActive },
        DisplayName = "Bridge Calls"
    )]
    public class BridgeCalls : Activity
    {
        private readonly ITelnyxClient _telnyxClient;

        public BridgeCalls(ITelnyxClient telnyxClient)
        {
            _telnyxClient = telnyxClient;
        }

        [ActivityProperty(Label = "Call Control ID A", Hint = "Unique identifier and token for controlling the call.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? CallControlIdA { get; set; } = default!;

        [ActivityProperty(Label = "Call Control ID B", Hint = "The Call Control ID of the call you want to bridge with.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string CallControlIdB { get; set; } = default!;

        [ActivityProperty(
            Label = "Command ID",
            Hint = "Use this field to avoid duplicate commands. Telnyx will ignore commands with the same Command ID.",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? CommandId { get; set; }

        [ActivityProperty(
            Hint = "Use this field to add state to every subsequent webhook. It must be a valid Base-64 encoded string.", 
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? ClientState { get; set; }

        [ActivityProperty(
            Label = "Park After Unbridged", 
            Hint = "HTTP request type used for Webhook URL", 
            UIHint = ActivityPropertyUIHints.Dropdown, 
            Options = new[] { "", "self" }, 
            Category = PropertyCategories.Advanced, 
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? ParkAfterUnbridged { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var callControlIdA = context.GetCallControlId(CallControlIdA);

            var request = new BridgeCallsRequest(
                CallControlIdB,
                ClientState,
                CommandId,
                ParkAfterUnbridged
            );

            try
            {
                await _telnyxClient.Calls.BridgeCallsAsync(callControlIdA, request, context.CancellationToken);
                return Done();
            }
            catch (ApiException e)
            {
                if (await e.CallIsNoLongerActiveAsync())
                    return Outcome(TelnyxOutcomeNames.CallIsNoLongerActive);

                throw new WorkflowException(e.Content ?? e.Message, e);
            }
        }
    }

    public static class BridgeCallsExtensions
    {
        public static ISetupActivity<BridgeCalls> WithCallControlIdA(this ISetupActivity<BridgeCalls> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.CallControlIdA, value);
        public static ISetupActivity<BridgeCalls> WithCallControlIdA(this ISetupActivity<BridgeCalls> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.CallControlIdA, value);
        public static ISetupActivity<BridgeCalls> WithCallControlIdA(this ISetupActivity<BridgeCalls> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.CallControlIdA, value);
        public static ISetupActivity<BridgeCalls> WithCallControlIdA(this ISetupActivity<BridgeCalls> setup, Func<string?> value) => setup.Set(x => x.CallControlIdA, value);
        public static ISetupActivity<BridgeCalls> WithCallControlIdA(this ISetupActivity<BridgeCalls> setup, string? value) => setup.Set(x => x.CallControlIdA, value);

        public static ISetupActivity<BridgeCalls> WithCallControlIdB(this ISetupActivity<BridgeCalls> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.CallControlIdB, value);
        public static ISetupActivity<BridgeCalls> WithCallControlIdB(this ISetupActivity<BridgeCalls> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.CallControlIdB, value);
        public static ISetupActivity<BridgeCalls> WithCallControlIdB(this ISetupActivity<BridgeCalls> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.CallControlIdB, value);
        public static ISetupActivity<BridgeCalls> WithCallControlIdB(this ISetupActivity<BridgeCalls> setup, Func<string?> value) => setup.Set(x => x.CallControlIdB, value);
        public static ISetupActivity<BridgeCalls> WithCallControlIdB(this ISetupActivity<BridgeCalls> setup, string? value) => setup.Set(x => x.CallControlIdB, value);

        public static ISetupActivity<BridgeCalls> WithCommandId(this ISetupActivity<BridgeCalls> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.CommandId, value);
        public static ISetupActivity<BridgeCalls> WithCommandId(this ISetupActivity<BridgeCalls> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.CommandId, value);
        public static ISetupActivity<BridgeCalls> WithCommandId(this ISetupActivity<BridgeCalls> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.CommandId, value);
        public static ISetupActivity<BridgeCalls> WithCommandId(this ISetupActivity<BridgeCalls> setup, Func<string?> value) => setup.Set(x => x.CommandId, value);
        public static ISetupActivity<BridgeCalls> WithCommandId(this ISetupActivity<BridgeCalls> setup, string? value) => setup.Set(x => x.CommandId, value);

        public static ISetupActivity<BridgeCalls> WithClientState(this ISetupActivity<BridgeCalls> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.ClientState, value);
        public static ISetupActivity<BridgeCalls> WithClientState(this ISetupActivity<BridgeCalls> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.ClientState, value);
        public static ISetupActivity<BridgeCalls> WithClientState(this ISetupActivity<BridgeCalls> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.ClientState, value);
        public static ISetupActivity<BridgeCalls> WithClientState(this ISetupActivity<BridgeCalls> setup, Func<string?> value) => setup.Set(x => x.ClientState, value);
        public static ISetupActivity<BridgeCalls> WithClientState(this ISetupActivity<BridgeCalls> setup, string? value) => setup.Set(x => x.ClientState, value);

        public static ISetupActivity<BridgeCalls> WithParkAfterUnbridged(this ISetupActivity<BridgeCalls> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.ParkAfterUnbridged, value);
        public static ISetupActivity<BridgeCalls> WithParkAfterUnbridged(this ISetupActivity<BridgeCalls> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.ParkAfterUnbridged, value);
        public static ISetupActivity<BridgeCalls> WithParkAfterUnbridged(this ISetupActivity<BridgeCalls> setup, Func<string?> value) => setup.Set(x => x.ParkAfterUnbridged, value);
        public static ISetupActivity<BridgeCalls> WithParkAfterUnbridged(this ISetupActivity<BridgeCalls> setup, string? value) => setup.Set(x => x.ParkAfterUnbridged, value);
    }
}