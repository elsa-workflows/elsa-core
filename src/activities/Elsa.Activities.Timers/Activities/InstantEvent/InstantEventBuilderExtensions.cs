using System;
using Elsa.Builders;
using Elsa.Services.Models;
using NodaTime;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Timers
{
    public static class InstantEventBuilderExtensions
    {
        public static IActivityBuilder InstantEvent(this IBuilder builder, Action<ISetupActivity<InstantEvent>>? setup = default) => builder.Then(setup);
        public static IActivityBuilder InstantEvent(this IBuilder builder, Func<ActivityExecutionContext, Instant> instant) => builder.InstantEvent(setup => setup.Set(x => x.Instant, instant));
        public static IActivityBuilder InstantEvent(this IBuilder builder, Func<Instant> instant) => builder.InstantEvent(setup => setup.Set(x => x.Instant, instant));
        public static IActivityBuilder InstantEvent(this IBuilder builder, Instant instant) => builder.InstantEvent(setup => setup.Set(x => x.Instant, instant));
    }
}