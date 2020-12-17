using System;

using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Timers
{
    public static class CancelTimerExtensions
    {
        public static IActivityBuilder CancelTimer(this IBuilder builder,
           Action<ISetupActivity<CancelTimer>>? setup = default) => builder.Then(setup);

        public static IActivityBuilder CancelTimer(this IBuilder builder,
             Func<ActivityExecutionContext, string> activityId) =>
             builder.CancelTimer(setup => setup.Set(x => x.ActivityId, activityId));

        public static IActivityBuilder CancelTimer(this IBuilder builder, Func<string> activityId) =>
            builder.CancelTimer(setup => setup.Set(x => x.ActivityId, activityId));

        public static IActivityBuilder CancelTimer(this IBuilder builder, string activityId) =>
            builder.CancelTimer(setup => setup.Set(x => x.ActivityId, activityId));
    }
}
