using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Telnyx.Client.Models;
using Elsa.Activities.Telnyx.Client.Services;
using Elsa.Activities.Telnyx.Webhooks.Models;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;
using Elsa.Activities.Temporal;
using Elsa.Attributes;
using Elsa.Builders;
using Elsa.Design;
using Elsa.Metadata;
using Elsa.Services;
using Elsa.Services.Models;
using NodaTime;

namespace Elsa.Activities.Telnyx.Activities
{
    [Action(
        Category = Constants.Category,
        Description = "Call a ring group.",
        Outcomes = new[] { "Connected", "No Response" },
        DisplayName = "Call Ring Group"
    )]
    public class CallRingGroup : CompositeActivity, IActivityPropertyDefaultValueProvider
    {
        [ActivityProperty(UIHint = ActivityPropertyUIHints.MultiText)]
        public IList<string> Extensions
        {
            get => GetState<IList<string>>(() => new List<string>());
            set => SetState(value);
        }

        [ActivityProperty(Label = "Call Control ID", Hint = "Unique identifier and token for controlling the call.", Category = PropertyCategories.Advanced)]
        public string? CallControlId
        {
            get => GetState<string>();
            set => SetState(value);
        }

        [ActivityProperty]
        public RingGroupStrategy Strategy
        {
            get => GetState<RingGroupStrategy>();
            set => SetState(value);
        }

        [ActivityProperty(Hint =
            "The 'from' number to be used as the caller id presented to the destination ('To' number). The number should be in +E164 format. This attribute will default to the 'From' number of the original call if omitted.")]
        public string? From
        {
            get => GetState<string>();
            set => SetState(value);
        }

        [ActivityProperty(Hint =
            "The string to be used as the caller id name (SIP From Display Name) presented to the destination ('To' number). The string should have a maximum of 128 characters, containing only letters, numbers, spaces, and -_~!.+ special characters. If omitted, the display name will be the same as the number in the 'From' field.")]
        public string? FromDisplayName
        {
            get => GetState<string>();
            set => SetState(value);
        }

        [ActivityProperty(DefaultValueProvider = typeof(CallRingGroup))]
        public Duration RingTime
        {
            get => GetState(() => Duration.FromSeconds(20));
            set => SetState(value);
        }
        
        private IList<string> CollectedCallControlIds
        {
            get => GetState(() => new List<string>());
            set => SetState(value);
        }

        public override void Build(ICompositeActivityBuilder builder) =>
            builder.Switch(cases =>
            {
                cases.Add(RingGroupStrategy.PrioritizedHunt.ToString(), () => Strategy == RingGroupStrategy.PrioritizedHunt, BuildPrioritizedHuntFlow);
                cases.Add(RingGroupStrategy.RingAll.ToString(), () => Strategy == RingGroupStrategy.RingAll, BuildRingAllFlow);
            });

        protected override async ValueTask OnExitAsync(ActivityExecutionContext context, object? output)
        {
            string? answeredCallControlId = null;
            
            if (output is CallAnsweredPayload callAnsweredPayload) 
                answeredCallControlId = callAnsweredPayload.CallControlId;
            
            // Hang up any pending calls.
            var outgoingCalls = CollectedCallControlIds.Where(x => x != answeredCallControlId).ToList();
            var client = context.GetService<ITelnyxClient>();

            foreach (var outgoingCall in outgoingCalls) 
                await client.Calls.HangupCallAsync(outgoingCall, new HangupCallRequest(null, null), context.CancellationToken);
        }

        private void BuildPrioritizedHuntFlow(IOutcomeBuilder builder) =>
            builder
                .ForEach(() => Extensions, iterate => iterate
                    .Then<ResolveExtension>(a => a.WithExtension(context => context.GetInput<string>()))
                    .Then<Dial>(a => a
                        .WithCallControlId(() => CallControlId)
                        .WithTo(context => context.GetInput<string>())
                        .WithTimeoutSecs(() => (int) RingTime.TotalSeconds)
                        .WithFrom(() => From)
                        .WithFromDisplayName(() => FromDisplayName)
                    )
                    .Then<Fork>(fork => fork.WithBranches("Connected", "No Response"), fork =>
                    {
                        fork
                            .When("Connected")
                            .ThenTypeNamed(CallAnsweredPayload.ActivityTypeName)
                            // Bridge
                            .Then<Finish>(finish => finish.WithOutcome("Connected").WithOutput(context => context.GetInput<TelnyxWebhook>()!.Data.Payload));

                        fork
                            .When("No Response")
                            .ThenTypeNamed(CallHangupPayload.ActivityTypeName);
                    })
                )
                .Finish("No Response");

        private void BuildRingAllFlow(IOutcomeBuilder builder) =>
            builder
                .Then<Fork>(fork => fork.WithBranches("Connected", "Timeout", "Dial Everyone"), fork =>
                {
                    fork
                        .When("Connected")
                        .ThenTypeNamed(CallAnsweredPayload.ActivityTypeName)
                        // Bridge
                        .Then<Finish>(finish => finish.WithOutcome("Connected").WithOutput(context => context.GetInput<TelnyxWebhook>()!.Data.Payload));

                    fork
                        .When("Timeout")
                        .StartIn(RingTime)
                        .Finish("No Response");

                    fork
                        .When("Dial Everyone")
                        .ParallelForEach(() => Extensions, iterate => iterate
                            .Then<ResolveExtension>(a => a.WithExtension(context => context.GetInput<string>()))
                            .Then<Dial>(a => a
                                .WithCallControlId(() => CallControlId)
                                .WithTo(context => context.GetInput<string>())
                                .WithTimeoutSecs(() => (int) RingTime.TotalSeconds)
                                .WithFrom(() => From)
                                .WithFromDisplayName(() => FromDisplayName)
                            )
                            .Then(CollectCallControlIds));

                });

        private void CollectCallControlIds(ActivityExecutionContext context)
        {
            var collection = CollectedCallControlIds;
            var callControlId = context.GetInput<DialResponse>()!.CallControlId;
            collection.Add(callControlId);
            CollectedCallControlIds = collection;
        }
        
        object IActivityPropertyDefaultValueProvider.GetDefaultValue(PropertyInfo property) => Duration.FromSeconds(20);
    }
}