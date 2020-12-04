using System;
using Elsa.Builders;
using Elsa.Services.Models;
using NodaTime;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Timers
{
    public static class TimerBuilderExtensions
    {
        public static IActivityBuilder Timer(this IBuilder builder,
            Action<ISetupActivity<Timer>>? setup = default) => builder.Then(setup);

        public static IActivityBuilder
            Timer(this IBuilder builder, Func<ActivityExecutionContext, Duration> value) =>
            builder.Timer(setup => setup.Set(x => x.Timeout, value));

        public static IActivityBuilder Timer(this IBuilder builder, Func<Duration> value) =>
            builder.Timer(setup => setup.Set(x => x.Timeout, value));

        public static IActivityBuilder Timer(this IBuilder builder, Duration value) =>
            builder.Timer(setup => setup.Set(x => x.Timeout, value));
    }
}