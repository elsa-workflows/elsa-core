using System;
using Elsa.Activities.Timers;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;
using NodaTime;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    public static class TimerEventExtensions
    {
        public static TimerEvent WithTimeout(this TimerEvent activity, IWorkflowExpression<Duration> value) => activity.With(x => x.Timeout, value);
        public static TimerEvent WithTimeout(this TimerEvent activity, Func<ActivityExecutionContext, Duration> value) => activity.With(x => x.Timeout, new CodeExpression<Duration>(value));
        public static TimerEvent WithTimeout(this TimerEvent activity, Func<Duration> value) => activity.With(x => x.Timeout, new CodeExpression<Duration>(value));
        public static TimerEvent WithTimeout(this TimerEvent activity, Duration value) => activity.With(x => x.Timeout, new CodeExpression<Duration>(value));
    }
}