using System;
using System.Threading.Tasks;
using Elsa.Activities.Temporal;
using Elsa.Builders;
using Elsa.Services.Models;
using NodaTime;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class StartAtExtensions
    {
        public static ISetupActivity<StartAt> WithInstant(this ISetupActivity<StartAt> activity, Func<ActivityExecutionContext, ValueTask<Instant>> value) => activity.Set(x => x.Instant, value!);
        public static ISetupActivity<StartAt> WithInstant(this ISetupActivity<StartAt> activity, Func<ActivityExecutionContext, Instant> value) => activity.Set(x => x.Instant, value);
        public static ISetupActivity<StartAt> WithInstant(this ISetupActivity<StartAt> activity, Func<Instant> value) => activity.Set(x => x.Instant, value);
        public static ISetupActivity<StartAt> WithInstant(this ISetupActivity<StartAt> activity, Instant value) => activity.Set(x => x.Instant, value);
    }
}