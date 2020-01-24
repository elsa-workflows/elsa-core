using System;
using Elsa.Activities.Timers;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;
using NodaTime;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    public static class TimerEventBuilderExtensions
    {
        public static ActivityBuilder TimerEvent(this IBuilder builder, Action<TimerEvent>? setup = default) => builder.Then(setup);
        public static ActivityBuilder TimerEvent(this IBuilder builder, IWorkflowExpression<Duration> value) => builder.TimerEvent(x => x.WithTimeout(value));
        public static ActivityBuilder TimerEvent(this IBuilder builder, Func<ActivityExecutionContext, Duration> value) => builder.TimerEvent(x => x.WithTimeout(value));
        public static ActivityBuilder TimerEvent(this IBuilder builder, Func<Duration> value) => builder.TimerEvent(x => x.WithTimeout(value));
        public static ActivityBuilder TimerEvent(this IBuilder builder, Duration value) => builder.TimerEvent(x => x.WithTimeout(value));
    }
}