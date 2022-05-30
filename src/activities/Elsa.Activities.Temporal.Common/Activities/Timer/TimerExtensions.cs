using System;
using System.Threading.Tasks;
using Elsa.Activities.Temporal;
using Elsa.Builders;
using Elsa.Services.Models;
using NodaTime;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class TimerExtensions
    {
        public static ISetupActivity<Timer> WithTimeout(this ISetupActivity<Timer> activity, Func<ActivityExecutionContext, ValueTask<Duration>> value) => activity.Set(x => x.Timeout, value!);
        public static ISetupActivity<Timer> WithTimeout(this ISetupActivity<Timer> activity, Func<ActivityExecutionContext, Duration> value) => activity.Set(x => x.Timeout, value);
        public static ISetupActivity<Timer> WithTimeout(this ISetupActivity<Timer> activity, Func<Duration> value) => activity.Set(x => x.Timeout, value);
        public static ISetupActivity<Timer> WithTimeout(this ISetupActivity<Timer> activity, Duration value) => activity.Set(x => x.Timeout, value);
    }
}