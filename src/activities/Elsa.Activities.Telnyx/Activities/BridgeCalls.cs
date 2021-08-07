using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Client.Models;
using Elsa.Activities.Telnyx.Client.Services;
using Elsa.Activities.Telnyx.Extensions;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;
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
        Outcomes = new[] {TelnyxOutcomeNames.Bridging, TelnyxOutcomeNames.Bridged, TelnyxOutcomeNames.LegABridged, TelnyxOutcomeNames.LegBBridged, OutcomeNames.Done, TelnyxOutcomeNames.CallIsNoLongerActive},
        DisplayName = "Bridge Calls"
    )]
    public class BridgeCalls : Activity
    {
        private readonly ITelnyxClient _telnyxClient;

        public BridgeCalls(ITelnyxClient telnyxClient)
        {
            _telnyxClient = telnyxClient;
        }

        [ActivityInput(Label = "Call Control ID A", Hint = "Unique identifier and token for controlling the call.", SupportedSyntaxes = new[] {SyntaxNames.JavaScript, SyntaxNames.Liquid})]
        public string? CallControlIdA { get; set; }

        [ActivityInput(Label = "Call Control ID B", Hint = "The Call Control ID of the call you want to bridge with.", SupportedSyntaxes = new[] {SyntaxNames.JavaScript, SyntaxNames.Liquid})]
        public string? CallControlIdB { get; set; }

        [ActivityInput(
            Label = "Command ID",
            Hint = "Use this field to avoid duplicate commands. Telnyx will ignore commands with the same Command ID.",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] {SyntaxNames.JavaScript, SyntaxNames.Liquid})]
        public string? CommandId { get; set; }

        [ActivityInput(
            Hint = "Use this field to add state to every subsequent webhook. It must be a valid Base-64 encoded string.",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] {SyntaxNames.JavaScript, SyntaxNames.Liquid})]
        public string? ClientState { get; set; }

        [ActivityInput(
            Label = "Park After Unbridged",
            Hint = "HTTP request type used for Webhook URL",
            UIHint = ActivityInputUIHints.Dropdown,
            Options = new[] {"", "self"},
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] {SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid})]
        public string? ParkAfterUnbridged { get; set; }

        [ActivityOutput] public CallBridgedPayload? CallBridgedPayloadA { get; set; }
        [ActivityOutput] public CallBridgedPayload? CallBridgedPayloadB { get; set; }
        [ActivityOutput] public CallBridgedPayload? Output { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            CallBridgedPayloadA = null;
            CallBridgedPayloadB = null;

            var callControlIdA = CallControlIdA = context.GetCallControlId(CallControlIdA);
            var callControlIdB = CallControlIdB = await GetCallControlBAsync(context);

            if (callControlIdB == null)
                throw new WorkflowException("Cannot bridge calls because the second leg's call control ID was not specified and no incoming activities provided this value");

            CallControlIdA = callControlIdA;

            var request = new BridgeCallsRequest(
                callControlIdB,
                ClientState,
                CommandId,
                ParkAfterUnbridged
            );

            try
            {
                await _telnyxClient.Calls.BridgeCallsAsync(callControlIdA, request, context.CancellationToken);
                return Combine(Outcome(TelnyxOutcomeNames.Bridging), Suspend());
            }
            catch (ApiException e)
            {
                if (await e.CallIsNoLongerActiveAsync())
                    return Outcome(TelnyxOutcomeNames.CallIsNoLongerActive);

                throw new WorkflowException(e.Content ?? e.Message, e);
            }
        }

        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context)
        {
            var payload = context.GetInput<CallBridgedPayload>()!;
            var results = new List<IActivityExecutionResult>();

            if (payload.CallControlId == CallControlIdA)
            {
                CallBridgedPayloadA = payload;
                Output = payload;
                results.Add(Outcome(TelnyxOutcomeNames.LegABridged));
            }

            if (payload.CallControlId == CallControlIdB)
            {
                CallBridgedPayloadB = payload;
                Output = payload;
                results.Add(Outcome(TelnyxOutcomeNames.LegBBridged));
            }

            if (CallBridgedPayloadA != null && CallBridgedPayloadB != null)
            {
                results.Add(Outcome(TelnyxOutcomeNames.Bridged, new BridgedCallsOutput(CallBridgedPayloadA, CallBridgedPayloadB)));
            }
            else
            {
                results.Add(Suspend());
            }

            return Combine(results);
        }

        private async Task<string?> GetCallControlBAsync(ActivityExecutionContext context)
        {
            if (!string.IsNullOrWhiteSpace(CallControlIdB))
                return CallControlIdB;

            if (context.Input is CallAnsweredPayload input)

                if (input != null)
                    return input.CallControlId;

            var inboundCallActivityId = context.WorkflowExecutionContext.GetInboundConnectionPath(Id).Where(x => x.Source.Activity.Type == nameof(Dial)).Select(x => x.Source.Activity.Id).FirstOrDefault();
            var inboundCallActivityResponse = inboundCallActivityId != null ? await context.WorkflowExecutionContext.GetActivityPropertyAsync<Dial, DialResponse>(inboundCallActivityId, x => x.DialResponse!) : default;
            return inboundCallActivityResponse != null ? inboundCallActivityResponse.CallControlId : null;
        }
    }

    public record BridgedCallsOutput(CallBridgedPayload PayloadA, CallBridgedPayload PayloadB);

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