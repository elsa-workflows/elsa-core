using System;
using Elsa.Activities.Timers;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    public static class CronEventBuilderExtensions
    {
        public static IActivityBuilder CronEvent(this IBuilder builder,
            Action<ISetupActivity<CronEvent>>? setup = default) => builder.Then(setup);

        public static IActivityBuilder CronEvent(this IBuilder builder,
            Func<ActivityExecutionContext, string> cronExpression) =>
            builder.CronEvent(setup => setup.Set(x => x.CronExpression, cronExpression));

        public static IActivityBuilder CronEvent(this IBuilder builder, Func<string> cronExpression) =>
            builder.CronEvent(setup => setup.Set(x => x.CronExpression, cronExpression));

        public static IActivityBuilder CronEvent(this IBuilder builder, string cronExpression) =>
            builder.CronEvent(setup => setup.Set(x => x.CronExpression, cronExpression));
    }
}