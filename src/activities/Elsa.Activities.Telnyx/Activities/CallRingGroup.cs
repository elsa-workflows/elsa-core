using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Telnyx.Client.Models;
using Elsa.Activities.Telnyx.Client.Services;
using Elsa.Activities.Telnyx.Models;
using Elsa.Activities.Telnyx.Services;
using Elsa.Activities.Telnyx.Webhooks.Models;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;
using Elsa.Activities.Temporal;
using Elsa.Attributes;
using Elsa.Builders;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Metadata;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.Logging;
using NodaTime;
using Refit;

namespace Elsa.Activities.Telnyx.Activities
{
    [Action(
        Category = Constants.Category,
        Description = "Call a ring group.",
        Outcomes = new[] {TelnyxOutcomeNames.Connected, TelnyxOutcomeNames.NoResponse},
        DisplayName = "Call Ring Group"
    )]
    public class CallRingGroup : CompositeActivity, IActivityPropertyDefaultValueProvider
    {
        private readonly ILogger<CallRingGroup> _logger;

        public CallRingGroup(ILogger<CallRingGroup> logger)
        {
            _logger = logger;
        }

        [ActivityInput(UIHint = ActivityInputUIHints.MultiText, DefaultSyntax = SyntaxNames.Json, SupportedSyntaxes = new[] {SyntaxNames.Json, SyntaxNames.JavaScript, SyntaxNames.Liquid})]
        public IList<string> Extensions
        {
            get => GetState<IList<string>>(() => new List<string>());
            set => SetState(value);
        }

        [ActivityInput(Label = "Call Control ID", Hint = "Unique identifier and token for controlling the call.", Category = PropertyCategories.Advanced, SupportedSyntaxes = new[] {SyntaxNames.JavaScript, SyntaxNames.Liquid})]
        public string CallControlId
        {
            get => GetState<string>()!;
            set => SetState(value);
        }

        [ActivityInput(
            Label = "Call Control App ID",
            Hint = "The ID of the Call Control App (formerly ID of the connection) to be used when dialing the destination.",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] {SyntaxNames.JavaScript, SyntaxNames.Liquid}
        )]
        public string? CallControlAppId
        {
            get => GetState<string>();
            set => SetState(value);
        }

        [ActivityInput(SupportedSyntaxes = new[] {SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid})]
        public RingGroupStrategy Strategy
        {
            get => GetState<RingGroupStrategy>();
            set => SetState(value);
        }

        [ActivityInput(
            Hint = "The 'from' number to be used as the caller id presented to the destination ('To' number). The number should be in +E164 format. This attribute will default to the 'From' number of the original call if omitted.",
            SupportedSyntaxes = new[] {SyntaxNames.JavaScript, SyntaxNames.Liquid}
        )]
        public string? From
        {
            get => GetState<string>();
            set => SetState(value);
        }

        [ActivityInput(
            Hint =
                "The string to be used as the caller id name (SIP From Display Name) presented to the destination ('To' number). The string should have a maximum of 128 characters, containing only letters, numbers, spaces, and -_~!.+ special characters. If omitted, the display name will be the same as the number in the 'From' field.",
            SupportedSyntaxes = new[] {SyntaxNames.JavaScript, SyntaxNames.Liquid}
        )]
        public string? FromDisplayName
        {
            get => GetState<string>();
            set => SetState(value);
        }

        [ActivityInput(DefaultValueProvider = typeof(CallRingGroup), SupportedSyntaxes = new[] {SyntaxNames.JavaScript, SyntaxNames.Liquid})]
        public Duration RingTime
        {
            get => GetState(() => Duration.FromSeconds(20));
            set => SetState(value);
        }

        [ActivityInput(
            Hint = "The maximum time to wait for anyone to pickup before giving up.",
            SupportedSyntaxes = new[] {SyntaxNames.JavaScript, SyntaxNames.Liquid}
        )]
        public Duration? MaxQueueWaitTime
        {
            get => GetState<Duration>();
            set => SetState(value);
        }

        [ActivityInput(
            Hint = "Enables Answering Machine Detection.",
            UIHint = ActivityInputUIHints.Dropdown,
            Options = new[] {"disabled", "detect", "detect_beep", "detect_words", "greeting_end"},
            DefaultValue = "disabled",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] {SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid})]
        public string? AnsweringMachineDetection { get; set; } = "disabled";

        private CallAnsweredPayload? CallAnsweredPayload
        {
            get => GetState<CallAnsweredPayload?>();
            set => SetState(value);
        }

        private IList<DialResponse> CollectedDialResponses
        {
            get => GetState(() => new List<DialResponse>());
            set => SetState(value);
        }

        public override void Build(ICompositeActivityBuilder builder) =>
            builder
                .StartWith<Fork>(fork => fork.WithBranches("Ring", "Queue Timeout"), fork =>
                {
                    fork.When("Ring")
                        .While(true, iterate => iterate
                            .Switch(cases =>
                            {
                                cases.Add(RingGroupStrategy.PrioritizedHunt.ToString(), () => Strategy == RingGroupStrategy.PrioritizedHunt, BuildPrioritizedHuntFlow);
                                cases.Add(RingGroupStrategy.RingAll.ToString(), () => Strategy == RingGroupStrategy.RingAll, BuildRingAllFlow);
                            }));

                    fork.When("Queue Timeout")
                        .IfFalse(() => IsNullOrZero(MaxQueueWaitTime), whenTrue =>
                        {
                            whenTrue
                                .Timer(() => MaxQueueWaitTime!.Value)
                                .Finish(TelnyxOutcomeNames.NoResponse);
                        });
                });

        protected override async ValueTask OnExitAsync(ActivityExecutionContext context, object? output)
        {
            // Hang up any pending calls.

            var answeredCallControlId = CallAnsweredPayload?.CallControlId;
            var outgoingCalls = CollectedDialResponses.Where(x => x.CallControlId != answeredCallControlId).ToList();
            var client = context.GetService<ITelnyxClient>();

            foreach (var outgoingCall in outgoingCalls)
            {
                try
                {
                    await client.Calls.HangupCallAsync(outgoingCall.CallControlId, new HangupCallRequest(null, null), context.CancellationToken);
                }
                catch (ApiException e)
                {
                    _logger.LogTrace(e, "Error while trying to hang up an outgoing call");
                }
            }
        }

        private void BuildPrioritizedHuntFlow(IOutcomeBuilder builder) =>
            builder
                .ForEach(() => Extensions, iterate => iterate
                    .Then<Dial>(dial => dial
                            .WithConnectionId(() => CallControlAppId)
                            .WithTo(ResolveExtensionAsync)
                            .WithTimeoutSecs(() => (int) RingTime.TotalSeconds)
                            .WithFrom(() => From)
                            .WithFromDisplayName(() => FromDisplayName),
                        dial =>
                        {
                            dial
                                .When(TelnyxOutcomeNames.Answered)
                                .Then<BridgeCalls>(bridgeCalls =>
                                    bridgeCalls.When(TelnyxOutcomeNames.Bridged)
                                        .Finish(activity => activity.WithOutcome(TelnyxOutcomeNames.Connected).WithOutput(context => context.GetInput<BridgedCallsOutput>())));
                        }
                    )
                )
                .IfTrue(() => IsNullOrZero(MaxQueueWaitTime), whenTrue => whenTrue.Finish(TelnyxOutcomeNames.NoResponse));

        private void BuildRingAllFlow(IOutcomeBuilder builder) =>
            builder
                .Then<Fork>(fork => fork.WithBranches("Connected", "Timeout", "Dial Everyone"), fork =>
                {
                    fork
                        .When(TelnyxOutcomeNames.Connected)
                        .ThenTypeNamed(CallAnsweredPayload.ActivityTypeName)
                        .Then(context => CallAnsweredPayload = (CallAnsweredPayload) context.GetInput<TelnyxWebhook>()!.Data.Payload)
                        .Then<BridgeCalls>(bridge => bridge
                            .WithCallControlIdA(() => CallControlId)
                            .WithCallControlIdB(() => CallAnsweredPayload!.CallControlId), bridge => bridge
                            .When(TelnyxOutcomeNames.Bridged)
                            .Finish(activity => activity.WithOutcome(TelnyxOutcomeNames.Connected).WithOutput(context => context.GetInput<BridgedCallsOutput>())));

                    fork
                        .When("Timeout")
                        .StartIn(() => RingTime)
                        .IfTrue(() => IsNullOrZero(MaxQueueWaitTime), whenTrue => whenTrue.Finish(TelnyxOutcomeNames.NoResponse));

                    fork
                        .When("Dial Everyone")
                        .ParallelForEach(() => Extensions, iterate => iterate
                            .Then<Dial>(a => a
                                .WithSuspendWorkflow(false)
                                .WithConnectionId(() => CallControlAppId)
                                .WithTo(ResolveExtensionAsync)
                                .WithTimeoutSecs(() => (int) RingTime.TotalSeconds)
                                .WithFrom(() => From)
                                .WithFromDisplayName(() => FromDisplayName)
                            )
                            .Then(CollectCallControlIds));
                });

        private void CollectCallControlIds(ActivityExecutionContext context)
        {
            var collection = CollectedDialResponses;
            var dialResponse = context.GetInput<DialResponse>()!;
            collection.Add(dialResponse);
            CollectedDialResponses = collection;
        }

        private static async ValueTask<string?> ResolveExtensionAsync(ActivityExecutionContext context)
        {
            if (context.Resuming)
                return await context.GetActivityPropertyAsync<Dial, string>(x => x.To)!;

            var extension = context.GetInput<string>()!;
            var extensionProvider = context.GetService<IExtensionProvider>();
            var resolvedExtension = await extensionProvider.GetAsync(extension, context.CancellationToken);
            return resolvedExtension?.Number ?? extension;
        }

        private static bool IsNullOrZero(Duration? duration) => duration == null || duration.Value == Duration.Zero;

        object IActivityPropertyDefaultValueProvider.GetDefaultValue(PropertyInfo property) => Duration.FromSeconds(20);
    }
}