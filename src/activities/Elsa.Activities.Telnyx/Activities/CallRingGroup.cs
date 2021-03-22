using System;
using System.Collections.Generic;
using System.Reflection;
using Elsa.Activities.ControlFlow;
using Elsa.Attributes;
using Elsa.Builders;
using Elsa.Design;
using Elsa.Metadata;
using Elsa.Services;
using NodaTime;

namespace Elsa.Activities.Telnyx.Activities
{
    [Action(
        Category = Constants.Category,
        Description = "Call a ring group.",
        Outcomes = new[] { "Connected", "NoResponse" },
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

        public override void Build(ICompositeActivityBuilder builder)
        {
            builder.Switch(cases =>
            {
                cases.Add(RingGroupStrategy.PrioritizedHunt.ToString(), () => Strategy == RingGroupStrategy.PrioritizedHunt, BuildSerialFlow);
                cases.Add(RingGroupStrategy.RingAll.ToString(), () => Strategy == RingGroupStrategy.RingAll, BuildRingAllFlow);
            });
        }

        private void BuildSerialFlow(IOutcomeBuilder builder)
        {
            builder
                .ForEach(() => Extensions, DialExtension);
        }

        private void BuildRingAllFlow(IOutcomeBuilder builder)
        {
            builder.Then(() => Console.Write("Let's do parallel"));
        }

        private void DialExtension(IOutcomeBuilder builder)
        {
            builder
                .Then<ResolveExtension>(a => a.WithExtension(context => context.GetInput<string>()))
                .Then<Dial>(a => a
                    .WithTo(context => context.GetInput<string>())
                    .WithFrom(() => From)
                    .WithFromDisplayName(() => FromDisplayName)
                )
                .Then();
        }

        object IActivityPropertyDefaultValueProvider.GetDefaultValue(PropertyInfo property) => Duration.FromSeconds(20);
    }
}