using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Telnyx.Client.Models;
using Elsa.Activities.Telnyx.Client.Services;
using Elsa.Activities.Telnyx.Extensions;
using Elsa.Activities.Telnyx.Models;
using Elsa.Activities.Telnyx.Services;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;
using Elsa.Activities.Temporal;
using Elsa.Attributes;
using Elsa.Builders;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Metadata;
using Elsa.Services;
using Elsa.Services.Models;
using Humanizer;
using Microsoft.Extensions.Logging;
using NodaTime;
using Refit;

namespace Elsa.Activities.Telnyx.Activities
{
    [Action(
        Category = Constants.Category,
        Description = "Call a ring group.",
        Outcomes = new[] { TelnyxOutcomeNames.Connected, TelnyxOutcomeNames.NoResponse },
        DisplayName = "Call Ring Group"
    )]
    public class CallRingGroup : CompositeActivity, IActivityPropertyDefaultValueProvider
    {
        private readonly ILogger _logger;

        public CallRingGroup(ILogger<CallRingGroup> logger)
        {
            _logger = logger;
        }

        [ActivityInput(UIHint = ActivityInputUIHints.MultiText, DefaultSyntax = SyntaxNames.Json, SupportedSyntaxes = new[] { SyntaxNames.Json, SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public IList<string> Extensions
        {
            get => GetState<IList<string>>(() => new List<string>());
            set => SetState(value);
        }

        [ActivityInput(Label = "Call Control ID", Hint = "Unique identifier and token for controlling the call.", Category = PropertyCategories.Advanced, SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string CallControlId
        {
            get => GetState<string>()!;
            set => SetState(value);
        }

        [ActivityInput(
            Label = "Call Control App ID",
            Hint = "The ID of the Call Control App (formerly ID of the connection) to be used when dialing the destination.",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? CallControlAppId
        {
            get => GetState<string>();
            set => SetState(value);
        }

        [ActivityInput(SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public RingGroupStrategy Strategy
        {
            get => GetState<RingGroupStrategy>();
            set => SetState(value);
        }

        [ActivityInput(
            Hint = "The 'from' number to be used as the caller id presented to the destination ('To' number). The number should be in +E164 format. This attribute will default to the 'From' number of the original call if omitted.",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? From
        {
            get => GetState<string>();
            set => SetState(value);
        }

        [ActivityInput(
            Hint =
                "The string to be used as the caller id name (SIP From Display Name) presented to the destination ('To' number). The string should have a maximum of 128 characters, containing only letters, numbers, spaces, and -_~!.+ special characters. If omitted, the display name will be the same as the number in the 'From' field.",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? FromDisplayName
        {
            get => GetState<string>();
            set => SetState(value);
        }

        [ActivityInput(DefaultValueProvider = typeof(CallRingGroup), SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public Duration RingTime
        {
            get => GetState(() => Duration.FromSeconds(20));
            set => SetState(value);
        }

        [ActivityInput(
            Hint = "The maximum time to wait for anyone to pickup before giving up.",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public Duration? MaxQueueWaitTime
        {
            get => GetState<Duration>();
            set => SetState(value);
        }

        [ActivityInput(
            Hint = "The audio file to play while dialing.",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public Uri? MusicOnHold
        {
            get => GetState<Uri>();
            set => SetState(value);
        }

        [ActivityInput(
            Hint = "The text to speak initially while extensions are being dialed.",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string TextToSpeak
        {
            get => GetState<string>(() => "Please hold while we are trying to connect you");
            set => SetState(value);
        }

        [ActivityInput(
            Hint = "The text to speak periodically while extensions are being dialed.",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string TextToSpeakWhileDialing
        {
            get => GetState<string>(() => "We're still trying to connect you. Please hold.");
            set => SetState(value);
        }

        [ActivityInput(
            Hint = "The interval that the text to speak while dialing should be spoken.",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid },
            DefaultValueProvider = typeof(CallRingGroup)
        )]
        public Duration? SpeakEvery
        {
            get => GetState(() => Duration.FromSeconds(30));
            set => SetState(value);
        }

        [ActivityInput(
            Hint = "Enables Answering Machine Detection.",
            UIHint = ActivityInputUIHints.Dropdown,
            Options = new[] { "disabled", "detect", "detect_beep", "detect_words", "greeting_end" },
            DefaultValue = "disabled",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? AnsweringMachineDetection { get; set; } = "disabled";

        [ActivityInput(
            Hint = "The text to speak when a user answers a call while the call was already bridged to another user.",
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid },
            DefaultValue = "Thank you for answering the call, however a coworker has already answered. The call will now be disconnected.")]
        public string AlreadyPickedUpText
        {
            get => GetState<string>(() => "Thank you for answering the call, however a coworker has already answered. The call will now be disconnected.");
            set => SetState(value);
        }

        private IList<CallAnsweredPayload> CallAnsweredPayloads
        {
            get => GetState<IList<CallAnsweredPayload>>(() => new List<CallAnsweredPayload>());
            set => SetState(value);
        }

        private IList<DialResponse> CollectedDialResponses
        {
            get => GetState(() => new List<DialResponse>());
            set => SetState(value);
        }

        private bool CallerHangup
        {
            get => GetState<bool>();
            set => SetState(value);
        }

        public override void Build(ICompositeActivityBuilder builder)
        {
            // Root.
            builder
                .Then<Fork>(fork => fork.WithBranches("Call", "Hold", "Hangup"), fork =>
                {
                    fork.When("Hangup")
                        .ThenTypeNamed(CallHangupPayload.ActivityTypeName).WithName("CallerHangupEvent")
                        .If(context => context.GetInput<CallHangupPayload>()!.CallSessionId == context.CorrelationId, @if =>
                        {
                            @if.When(OutcomeNames.True)
                                .Then(() => CallerHangup = true)
                                .ThenNamed("ExitWithNoResponse");

                            @if.When(OutcomeNames.False)
                                .ThenNamed("CallerHangupEvent");
                        });

                    fork
                        .When("Hold")
                        .Then<SpeakText>(speakText => speakText.WithPayload(() => TextToSpeak), speakText =>
                        {
                            speakText.When(TelnyxOutcomeNames.CallIsNoLongerActive).ThenNamed("ExitWithNoResponse");

                            speakText.When(TelnyxOutcomeNames.FinishedSpeaking)
                                .If(() => MusicOnHold != null, @if =>
                                {
                                    @if
                                        .When(OutcomeNames.True)
                                        .Then<PlayAudio>(playAudio => playAudio.WithAudioUrl(() => MusicOnHold).WithLoop("infinity"));
                                }).WithName("PleaseHold")
                                .Timer(() => SpeakEvery ?? Duration.FromSeconds(30))
                                .If(() => MusicOnHold != null, @if =>
                                {
                                    @if.When(OutcomeNames.True)
                                        .Then<StopAudioPlayback>(stopAudioPlayback =>
                                        {
                                            stopAudioPlayback.When(TelnyxOutcomeNames.CallIsNoLongerActive).ThenNamed("ExitWithNoResponse");
                                            stopAudioPlayback.When(TelnyxOutcomeNames.CallPlaybackEnded).ThenNamed("SpeakPleaseHold");
                                        });

                                    @if.When(OutcomeNames.False).ThenNamed("SpeakPleaseHold");
                                })
                                .Add<SpeakText>(speakText2 => speakText2.WithPayload(() => TextToSpeakWhileDialing), speakText2 =>
                                {
                                    speakText2.When(TelnyxOutcomeNames.CallIsNoLongerActive).ThenNamed("ExitWithNoResponse");
                                    speakText2.When(TelnyxOutcomeNames.FinishedSpeaking).ThenNamed("PleaseHold");
                                }).WithName("SpeakPleaseHold");
                        });

                    fork.When("Call")
                        .Then<Fork>(innerFork => innerFork.WithBranches("Ring", "Queue Timeout"), innerFork =>
                        {
                            innerFork.When("Ring")
                                .While(() => !CallerHangup, iterate => iterate
                                    .Switch(cases =>
                                    {
                                        cases.Add(RingGroupStrategy.PrioritizedHunt.ToString(), () => Strategy == RingGroupStrategy.PrioritizedHunt, BuildPrioritizedHuntFlow);
                                        cases.Add(RingGroupStrategy.RingAll.ToString(), () => Strategy == RingGroupStrategy.RingAll, BuildRingAllFlow);
                                    }))
                                .IfTrue(() => IsNullOrZero(MaxQueueWaitTime), ifTrue => ifTrue.ThenNamed("ExitWithNoResponse"));

                            innerFork.When("Queue Timeout")
                                .IfFalse(() => IsNullOrZero(MaxQueueWaitTime), ifFalse => ifFalse.StartIn(() => MaxQueueWaitTime!.Value).ThenNamed("ExitWithNoResponse"));
                        });
                });

            // No Response exit node.
            builder.Add<If>(@if => @if.WithCondition(() => MusicOnHold != null), @if =>
                {
                    @if.When(OutcomeNames.True)
                        .Then<StopAudioPlayback>(stopAudioPlayback =>
                        {
                            stopAudioPlayback
                                .When(TelnyxOutcomeNames.CallPlaybackEnded)
                                .Finish(TelnyxOutcomeNames.NoResponse);

                            stopAudioPlayback
                                .When(TelnyxOutcomeNames.CallIsNoLongerActive)
                                .Finish(TelnyxOutcomeNames.NoResponse);
                        });

                    @if.When(OutcomeNames.False).Finish(TelnyxOutcomeNames.NoResponse);
                })
                .WithName("ExitWithNoResponse");
        }

        private void BuildPrioritizedHuntFlow(IOutcomeBuilder builder) =>
            builder
                .ForEach(() => Extensions, iterate => iterate
                    .If(() => CallerHangup, @if =>
                    {
                        @if.When(OutcomeNames.True)
                            .Break();

                        @if.When(OutcomeNames.False)
                            .Then<Dial>(dial => dial
                                    .WithConnectionId(() => CallControlAppId)
                                    .WithTo(ResolveExtensionAsync)
                                    .WithTimeoutSecs(() => (int)RingTime.TotalSeconds)
                                    .WithFrom(() => From)
                                    .WithFromDisplayName(() => FromDisplayName.SanitizeCallerName()),
                                dial =>
                                {
                                    dial
                                        .When(TelnyxOutcomeNames.Answered)
                                        .If(() => MusicOnHold != null, @if =>
                                        {
                                            @if.When(OutcomeNames.True).Then<StopAudioPlayback>();
                                            @if.When(OutcomeNames.False).ThenNamed("BridgeCalls1");
                                        })
                                        .Add<BridgeCalls>(branch: bridgeCalls =>
                                            bridgeCalls.When(TelnyxOutcomeNames.Bridged)
                                                .Finish(activity => activity.WithOutcome(TelnyxOutcomeNames.Connected).WithOutput(context => context.GetInput<BridgedCallsOutput>())))
                                        .WithName("BridgeCalls1");
                                }
                            );
                    })
                )
                // If we don't have a queue wait time, then break out of outer loop.
                .IfTrue(() => IsNullOrZero(MaxQueueWaitTime), @if => @if.Break());

        private void BuildRingAllFlow(IOutcomeBuilder builder) =>
            builder
                .Then<Fork>(fork => fork.WithBranches("Connected", "Timeout", "Dial Everyone"), fork =>
                {
                    fork
                        .When(TelnyxOutcomeNames.Connected)
                        .ThenTypeNamed(CallAnsweredPayload.ActivityTypeName)
                        .Then(context =>
                        {
                            var payload = context.GetInput<CallAnsweredPayload>()!;
                            CallAnsweredPayloads.Add(payload);
                        })
                        .If(() => CallAnsweredPayloads.Count >= 1,
                            whenTrue =>
                            {
                                // The call was already answered, so play a friendly message indicating this fact and then hang up.
                                whenTrue.Then<SpeakText>(speakText => speakText.WithPayload(() => AlreadyPickedUpText),
                                    speakText =>
                                    {
                                        speakText.When(TelnyxOutcomeNames.FinishedSpeaking)
                                            .Then<HangupCall>(hangupCall => hangupCall
                                                .Set(x => x.CallControlId, () => CallAnsweredPayloads.Last().CallControlId));
                                    });
                            },
                            whenFalse => whenFalse
                                .If(() => MusicOnHold != null, @if => @if.When(OutcomeNames.True).Then<StopAudioPlayback>())
                                .Then<BridgeCalls>(bridge => bridge
                                    .WithCallControlIdA(() => CallControlId)
                                    .WithCallControlIdB(() => CallAnsweredPayloads.First().CallControlId), bridge =>
                                {
                                    bridge
                                        .When(TelnyxOutcomeNames.Bridged)
                                        .Then(async context => await CancelPendingCallsAsync(context))
                                        .Finish(activity => activity.WithOutcome(TelnyxOutcomeNames.Connected).WithOutput(context => context.GetInput<BridgedCallsOutput>()));
                                }).WithName("BridgeCalls2"));

                    fork
                        .When("Timeout")
                        .StartIn(() => RingTime)

                        // If we don't have a queue wait time, then break out of outer loop.
                        .If(@if => @if.WithCondition(() => IsNullOrZero(MaxQueueWaitTime)), @if => { @if.When(OutcomeNames.True).Break(); });

                    fork
                        .When("Dial Everyone")
                        .ParallelForEach(() => Extensions, iterate => iterate
                            .Then<Dial>(a => a
                                .WithSuspendWorkflow(false)
                                .WithConnectionId(() => CallControlAppId)
                                .WithTo(ResolveExtensionAsync)
                                .WithTimeoutSecs(() => (int)RingTime.TotalSeconds)
                                .WithFrom(() => From)
                                .WithFromDisplayName(() => FromDisplayName.SanitizeCallerName())
                            )
                            .Then(CollectCallControlIds));
                });

        private void CollectCallControlIds(ActivityExecutionContext context)
        {
            var collection = CollectedDialResponses;
            var dialResponse = context.GetInput<DialResponse>()!;

            collection.Add(dialResponse);
            CollectedDialResponses = collection;
            context.JournalData.Add("Collected Dial Responses", collection);
        }

        private async Task CancelPendingCallsAsync(ActivityExecutionContext context)
        {
            var answeredCallControlIds = CallAnsweredPayloads.Select(x => x.CallControlId).ToHashSet();
            var outgoingCalls = CollectedDialResponses.Where(x => !answeredCallControlIds.Contains(x.CallControlId)).ToList();
            var client = context.GetService<ITelnyxClient>();

            context.JournalData.Add("Outgoing Calls", outgoingCalls);

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

        private static async ValueTask<string?> ResolveExtensionAsync(ActivityExecutionContext context)
        {
            if (context.Resuming)
                return await context.GetActivityPropertyAsync<Dial, string>(x => x.To)!;

            var extension = context.GetInput<string>()!;
            var extensionProvider = context.GetService<IExtensionProvider>();
            var resolvedExtension = await extensionProvider.GetAsync(extension, context.CancellationToken);
            return resolvedExtension?.Destination ?? extension;
        }

        private static bool IsNullOrZero(Duration? duration) => duration == null || duration.Value == Duration.Zero;

        object IActivityPropertyDefaultValueProvider.GetDefaultValue(PropertyInfo property) =>
            property.Name switch
            {
                nameof(SpeakEvery) => Duration.FromSeconds(30),
                nameof(RingTime) => Duration.FromSeconds(20),
                _ => default!
            };
    }
}