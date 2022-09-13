using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
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
using Elsa.Scripting.JavaScript.Services;
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

        [ActivityInput(UIHint = ActivityInputUIHints.CodeEditor, DefaultSyntax = SyntaxNames.JavaScript, SupportedSyntaxes = new[] { SyntaxNames.JavaScript })]
        public string? ExtensionsExpression
        {
            get => GetState<string>();
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

        [ActivityInput(
            Hint = "Allow the caller to leave a message instead of waiting to be connected.",
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public bool AllowLeaveMessage
        {
            get => GetState<bool>();
            set => SetState(value);
        }

        [ActivityInput(
            Hint = "The digit (between 0 and 9) the caller should press if they want to leave a message.",
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public int LeaveMessageDigit
        {
            get => GetState<int>();
            set => SetState(value);
        }

        [ActivityInput(
            Hint = "The text to speak when a user chose to leave a message.",
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid },
            DefaultValue = "Please leave a message at the beep."
        )]
        public string LeaveMessageText
        {
            get => GetState<string>(() => "Please leave a message at the beep.");
            set => SetState(value);
        }

        [ActivityOutput]
        public CallRecordingSaved? SavedMessageRecording
        {
            get => GetState<CallRecordingSaved>();
            set => SetState(value);
        }

        private IDictionary<string, CallAnsweredPayload> CallAnsweredPayloads
        {
            get => GetState(() => new Dictionary<string, CallAnsweredPayload>());
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

        private bool LeavingMessage
        {
            get => GetState<bool>();
            set => SetState(value);
        }

        private bool Bridged
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
                                .Then(async context =>
                                {
                                    var payload = (CallHangupPayload)(await context.GetNamedActivityPropertyAsync("CallerHangupEvent", "Output"))!;
                                    CallAnsweredPayloads.Remove(payload.To);
                                })
                                .ThenNamed("CallerHangupEvent");
                        });

                    fork
                        .When("Hold")
                        .Then<Fork>(fork => fork.WithBranches("Loop", "Dtmf Received"), fork =>
                        {
                            fork.When("Dtmf Received")
                                .ThenTypeNamed(CallDtmfReceivedPayload.ActivityTypeName).WithName("CallDtmfReceived")
                                .If(context => context.GetInput<CallDtmfReceivedPayload>()!.Digit == LeaveMessageDigit.ToString(),
                                    ifTrue =>
                                    {
                                        ifTrue
                                            .Then(() => LeavingMessage = true).WithDisplayName("LeavingMessage = True")
                                            .Then(async context => await CancelPendingCallsAsync(context))
                                            .Then<Fork>(fork => fork.WithBranches("Stop Music", "Leave Message"), fork =>
                                            {
                                                fork.When("Stop Music")
                                                    .Then<StopAudioPlayback>(stopAudioPlayback => stopAudioPlayback.When(TelnyxOutcomeNames.CallIsNoLongerActive)
                                                        .IfFalse(() => LeavingMessage, ifFalse => ifFalse.ThenNamed("ExitWithNoResponse")));

                                                fork.When("Leave Message")
                                                    .Then<SpeakText>(speakText => speakText.WithPayload(() => LeaveMessageText), speakText =>
                                                    {
                                                        speakText.When(TelnyxOutcomeNames.CallIsNoLongerActive).ThenNamed("ExitWithNoResponse");

                                                        speakText.When(TelnyxOutcomeNames.FinishedSpeaking)
                                                            .Then<StartRecording>(startRecording => startRecording.WithPlayBeep(true), startRecording =>
                                                            {
                                                                startRecording.When(TelnyxOutcomeNames.CallIsNoLongerActive).ThenNamed("ExitWithNoResponse");

                                                                startRecording.When(TelnyxOutcomeNames.FinishedRecording)
                                                                    .Then(async context =>
                                                                    {
                                                                        var recordingPayload = await context.GetNamedActivityPropertyAsync<StartRecording, CallRecordingSaved>("StartRecordingMessage", x => x.SavedRecordingPayload!);
                                                                        SavedMessageRecording = recordingPayload;
                                                                    })
                                                                    .Finish(TelnyxOutcomeNames.NoResponse);
                                                            }).WithName("StartRecordingMessage");
                                                    });
                                            });
                                    },
                                    ifFalse => { ifFalse.ThenNamed("CallDtmfReceived"); })
                                .ThenNamed("CallDtmfReceived");

                            fork.When("Loop")
                                .Then<SpeakText>(speakText => speakText.WithPayload(() => TextToSpeak), speakText =>
                                {
                                    speakText.When(TelnyxOutcomeNames.CallIsNoLongerActive).ThenNamed("ExitWithNoResponse");

                                    speakText.When(TelnyxOutcomeNames.FinishedSpeaking)
                                        .If(() => MusicOnHold != null && !LeavingMessage, @if =>
                                        {
                                            @if
                                                .When(OutcomeNames.True)
                                                .Then<PlayAudio>(playAudio => playAudio.WithAudioUrl(() => MusicOnHold).WithLoop("infinity"), playAudio => playAudio
                                                    .When(TelnyxOutcomeNames.CallIsNoLongerActive)
                                                    .ThenNamed("ExitWithNoResponse"));
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
            builder
                .Add(async context => await CancelPendingCallsAsync(context)).WithName("ExitWithNoResponse").WithDisplayName("Cancel Pending Calls 2")
                .Then<If>(@if => @if.WithCondition(() => MusicOnHold != null), @if =>
                {
                    @if.When(OutcomeNames.True)
                        .Then<StopAudioPlayback>(stopAudioPlayback =>
                        {
                            stopAudioPlayback
                                .When(TelnyxOutcomeNames.CallPlaybackEnded)
                                .IfFalse(() => LeavingMessage, ifFalse => ifFalse.Finish(TelnyxOutcomeNames.NoResponse));

                            stopAudioPlayback
                                .When(TelnyxOutcomeNames.CallIsNoLongerActive)
                                .If(() => LeavingMessage,
                                    ifTrue => ifTrue.Timer(Duration.FromMinutes(1)), // HACK: For some reason, the composite activity finishes too early before the recording is finished. 
                                    ifFalse => ifFalse.Finish(TelnyxOutcomeNames.NoResponse));
                        });

                    @if.When(OutcomeNames.False)
                        .IfFalse(() => LeavingMessage, ifFalse => ifFalse.Finish(TelnyxOutcomeNames.NoResponse));
                });
        }

        private void BuildPrioritizedHuntFlow(IOutcomeBuilder builder) =>
            builder
                .ForEach(async context => await EvaluateExtensionNumbersAsync(context), iterate => iterate
                    .If(() => CallerHangup || LeavingMessage, @if =>
                    {
                        @if.When(OutcomeNames.True)
                            .Break();

                        @if.When(OutcomeNames.False)
                            .Then<Dial>(dial => dial
                                    .WithConnectionId(() => CallControlAppId)
                                    .WithTo(context => context.GetVariable<string>("CurrentValue"))
                                    .WithTimeoutSecs(() => (int)RingTime.TotalSeconds)
                                    .WithFrom(() => From)
                                    .WithFromDisplayName(() => FromDisplayName),
                                dial =>
                                {
                                    dial
                                        .When(TelnyxOutcomeNames.Answered)
                                        .Then<StopAudioPlayback>(stopAudioPlayback => { stopAudioPlayback.When(TelnyxOutcomeNames.CallIsNoLongerActive).ThenNamed("FinishPrioritizedHunt"); })
                                        .Then<BridgeCalls>(branch: bridgeCalls =>
                                        {
                                            bridgeCalls.When(TelnyxOutcomeNames.Bridged).ThenNamed("FinishPrioritizedHunt");
                                            bridgeCalls.When(TelnyxOutcomeNames.CallIsNoLongerActive).ThenNamed("FinishPrioritizedHunt");
                                        })
                                        .WithName("BridgeCalls1");

                                    dial.Add<Finish>(activity => activity.WithOutcome(TelnyxOutcomeNames.Connected).WithOutput(context => context.GetInput<BridgedCallsOutput>())).WithName("FinishPrioritizedHunt");
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
                        .When("Connected")
                        .ThenTypeNamed(CallAnsweredPayload.ActivityTypeName)
                        .Then(context =>
                        {
                            var payload = context.GetInput<CallAnsweredPayload>()!;

                            // In case this event is received due to e.g. redelivery attempts from Telnyx, we need to make sure this is not the initial "answered" event.
                            if (payload.CallLegId != context.GetCallLegId(""))
                                CallAnsweredPayloads[payload.To] = payload;
                        })
                        .If(() => CallAnsweredPayloads.Count > 1,
                            whenTrue =>
                            {
                                // The call was already answered, so play a friendly message indicating this fact and then hang up.
                                whenTrue
                                    .Then<SpeakText>(speakText => speakText.WithCallControlId(() => CallAnsweredPayloads.Last().Value.CallControlId).WithPayload(() => AlreadyPickedUpText),
                                        speakText =>
                                        {
                                            speakText.When(TelnyxOutcomeNames.FinishedSpeaking)
                                                .Then<HangupCall>(
                                                    hangupCall => hangupCall.Set(x => x.CallControlId, () => CallAnsweredPayloads.Last().Value.CallControlId),
                                                    hangupCall => { hangupCall.When(TelnyxOutcomeNames.CallIsNoLongerActive).ThenNamed("CancelPendingCalls1"); });

                                            speakText.When(TelnyxOutcomeNames.CallIsNoLongerActive).ThenNamed("CancelPendingCalls1");
                                        });
                            },
                            whenFalse => whenFalse
                                .Then<StopAudioPlayback>(stopAudioPlayback => stopAudioPlayback.When(TelnyxOutcomeNames.CallIsNoLongerActive).ThenNamed("CancelPendingCalls1"))
                                .If(() => LeavingMessage,
                                    ifTrue => ifTrue.Break(),
                                    ifFalse =>
                                        ifFalse
                                            .IfTrue(() => CallAnsweredPayloads.Any(), ifTrue => ifTrue
                                                .Then<BridgeCalls>(bridge => bridge
                                                    .WithCallControlIdA(() => CallControlId)
                                                    .WithCallControlIdB(() => CallAnsweredPayloads.First().Value.CallControlId), bridge =>
                                                {
                                                    bridge
                                                        .When(TelnyxOutcomeNames.Bridged)
                                                        .Then(() => Bridged = true)
                                                        .ThenNamed("CancelPendingCalls1");

                                                    bridge
                                                        .When(TelnyxOutcomeNames.CallIsNoLongerActive).ThenNamed("CancelPendingCalls1");

                                                    bridge
                                                        .Add(async context => await CancelPendingCallsAsync(context)).WithName("CancelPendingCalls1").WithDisplayName("Cancel Pending Calls")
                                                        .Finish(activity => activity.WithOutcome(TelnyxOutcomeNames.Connected).WithOutput(async context =>
                                                        {
                                                            var output = await context.GetNamedActivityPropertyAsync<BridgeCalls, BridgedCallsOutput>("BridgeCalls2", x => x.Output!);
                                                            return output;
                                                        }));
                                                }).WithName("BridgeCalls2"))));
                    ;

                    fork
                        .When("Timeout")
                        .If(() => LeavingMessage || CallAnsweredPayloads.Any(),
                            ifTrue => ifTrue.Break(),
                            ifFalse => ifFalse
                                .StartIn(() => RingTime)

                                // If we don't have a queue wait time, then break out of outer loop.
                                .If(@if => @if.WithCondition(() => IsNullOrZero(MaxQueueWaitTime)), @if => { @if.When(OutcomeNames.True).Break(); }));

                    fork
                        .When("Dial Everyone")
                        .IfFalse(() => Bridged, ifFalse =>
                            ifFalse.ParallelForEach(async context => await EvaluateExtensionNumbersAsync(context), iterate => iterate
                                .Then<Dial>(a => a
                                    .WithSuspendWorkflow(false)
                                    .WithConnectionId(() => CallControlAppId)
                                    .WithTo(context => context.Input as string)
                                    .WithTimeoutSecs(() => (int)RingTime.TotalSeconds)
                                    .WithFrom(() => From)
                                    .WithFromDisplayName(() => FromDisplayName)
                                )
                                .Then(CollectCallControlIds)));
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
            var answeredCallControlIds = CallAnsweredPayloads.Select(x => x.Value.CallControlId).ToHashSet();
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

            CallAnsweredPayloads.Clear();
        }

        private async Task<ICollection<string>> EvaluateExtensionNumbersAsync(ActivityExecutionContext context)
        {
            var expression = ExtensionsExpression;

            if (string.IsNullOrWhiteSpace(expression))
                return await ResolveExtensionsAsync(context, Extensions);

            var javaScriptService = context.GetService<IJavaScriptService>();
            var extensions = (ICollection<string>?)await javaScriptService.EvaluateAsync(expression, Extensions.GetType(), context);

            return await ResolveExtensionsAsync(context, extensions ?? Array.Empty<string>());
        }

        private static async Task<ICollection<string>> ResolveExtensionsAsync(ActivityExecutionContext context, IEnumerable<string> extensions)
        {
            var numbers = new List<string>();

            foreach (var extension in extensions)
            {
                var number = await ResolveExtensionAsync(context, extension);

                if (!string.IsNullOrWhiteSpace(number))
                    numbers.Add(number);
            }

            return numbers;
        }

        private static async Task<string?> ResolveExtensionAsync(ActivityExecutionContext context, string extension)
        {
            var extensionProvider = context.GetService<IExtensionProvider>();
            var resolvedExtension = await extensionProvider.GetAsync(extension, context.CancellationToken);
            return resolvedExtension?.Destination;
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