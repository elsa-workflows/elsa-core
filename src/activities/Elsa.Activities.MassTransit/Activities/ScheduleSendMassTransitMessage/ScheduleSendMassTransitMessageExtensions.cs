using System;
using Elsa.Builders;
using Elsa.Services.Models;
using NodaTime;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    public static class ScheduleSendMassTransitMessageExtensions
    {
        public static ScheduleSendMassTransitMessage WithMessage(this ScheduleSendMassTransitMessage activity, Func<ActivityExecutionContext, object> value) => activity.With(x => x.Message, value);
        public static ScheduleSendMassTransitMessage WithMessage(this ScheduleSendMassTransitMessage activity, Func<object> value) => activity.With(x => x.Message, value);
        public static ScheduleSendMassTransitMessage WithMessage(this ScheduleSendMassTransitMessage activity, object value) => activity.With(x => x.Message, value);
        public static ScheduleSendMassTransitMessage WithEndpointAddress(this ScheduleSendMassTransitMessage activity, Uri value) => activity.With(x => x.EndpointAddress, value);
        public static ScheduleSendMassTransitMessage WithScheduledTime(this ScheduleSendMassTransitMessage activity, Func<ActivityExecutionContext, Instant> value) => activity.With(x => x.ScheduledTime, value);
        public static ScheduleSendMassTransitMessage WithScheduledTime(this ScheduleSendMassTransitMessage activity, Func<Instant> value) => activity.With(x => x.ScheduledTime, value);
        public static ScheduleSendMassTransitMessage WithScheduledTime(this ScheduleSendMassTransitMessage activity, Instant value) => activity.With(x => x.ScheduledTime, value);
    }
}