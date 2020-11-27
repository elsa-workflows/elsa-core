using System;
using Elsa.Builders;
using Elsa.Services.Models;
using NodaTime;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Timers
{
    public static class StartAtBuilderExtensions
    {
        public static IActivityBuilder StartAt(this IBuilder builder, Action<ISetupActivity<StartAt>>? setup = default) => builder.Then(setup);
        public static IActivityBuilder StartAt(this IBuilder builder, Func<ActivityExecutionContext, Instant> instant) => builder.StartAt(setup => setup.Set(x => x.Instant, instant));
        public static IActivityBuilder StartAt(this IBuilder builder, Func<Instant> instant) => builder.StartAt(setup => setup.Set(x => x.Instant, instant));
        public static IActivityBuilder StartAt(this IBuilder builder, Instant instant) => builder.StartAt(setup => setup.Set(x => x.Instant, instant));
        
        public static IActivityBuilder StartIn(this IBuilder builder, Func<ActivityExecutionContext, Duration> timeout) => builder.StartAt(context => Now(context).Plus(timeout(context)));
        public static IActivityBuilder StartIn(this IBuilder builder, Func<Duration> timeout) => builder.StartAt(context => Now(context).Plus(timeout()));
        public static IActivityBuilder StartIn(this IBuilder builder, Duration timeout) => builder.StartAt(context => Now(context).Plus(timeout));

        private static Instant Now(ActivityExecutionContext context) => context.GetService<IClock>().GetCurrentInstant();
    }
}