using System;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Timers
{
    public static class CronBuilderExtensions
    {
        public static IActivityBuilder Cron(this IBuilder builder,
            Action<ISetupActivity<Cron>>? setup = default) => builder.Then(setup);

        public static IActivityBuilder Cron(this IBuilder builder,
            Func<ActivityExecutionContext, string> cronExpression) =>
            builder.Cron(setup => setup.Set(x => x.CronExpression, cronExpression));

        public static IActivityBuilder Cron(this IBuilder builder, Func<string> cronExpression) =>
            builder.Cron(setup => setup.Set(x => x.CronExpression, cronExpression));

        public static IActivityBuilder Cron(this IBuilder builder, string cronExpression) =>
            builder.Cron(setup => setup.Set(x => x.CronExpression, cronExpression));
    }
}