using System;
using Elsa.Activities.Timers;
using Elsa.Builders;
using Elsa.Services.Models;
using NodaTime;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    public static class TimerEventBuilderExtensions
    {
        public static IActivityBuilder TimerEvent(this IBuilder builder,
            Action<ISetupActivity<TimerEvent>>? setup = default) => builder.Then(setup);

        public static IActivityBuilder
            TimerEvent(this IBuilder builder, Func<ActivityExecutionContext, Duration> value) =>
            builder.TimerEvent(setup => setup.Set(x => x.Timeout, value));

        public static IActivityBuilder TimerEvent(this IBuilder builder, Func<Duration> value) =>
            builder.TimerEvent(setup => setup.Set(x => x.Timeout, value));

        public static IActivityBuilder TimerEvent(this IBuilder builder, Duration value) =>
            builder.TimerEvent(setup => setup.Set(x => x.Timeout, value));
    }
}