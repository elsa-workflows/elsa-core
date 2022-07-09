using System;
using System.Threading.Tasks;
using Elsa.Activities.Temporal;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class CronExtensions
    {
        public static ISetupActivity<Cron> WithCronExpression(this ISetupActivity<Cron> activity, Func<ActivityExecutionContext, ValueTask<string>> value) => activity.Set(x => x.CronExpression, value!);
        public static ISetupActivity<Cron> WithCronExpression(this ISetupActivity<Cron> activity, Func<ActivityExecutionContext, string> value) => activity.Set(x => x.CronExpression, value);
        public static ISetupActivity<Cron> WithCronExpression(this ISetupActivity<Cron> activity, Func<string> value) => activity.Set(x => x.CronExpression, value);
        public static ISetupActivity<Cron> WithCronExpression(this ISetupActivity<Cron> activity, string value) => activity.Set(x => x.CronExpression, value);
    }
}