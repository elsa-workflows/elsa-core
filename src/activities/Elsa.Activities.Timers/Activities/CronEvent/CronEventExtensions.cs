using System;
using Elsa.Activities.Timers;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    public static class CronEventExtensions
    {
        public static CronEvent WithCronExpression(this CronEvent activity, IWorkflowExpression<string> value) => activity.With(x => x.CronExpression, value);
        public static CronEvent WithCronExpression(this CronEvent activity, Func<ActivityExecutionContext, string> value) => activity.With(x => x.CronExpression, new CodeExpression<string>(value));
        public static CronEvent WithCronExpression(this CronEvent activity, Func<string> value) => activity.With(x => x.CronExpression, new CodeExpression<string>(value));
        public static CronEvent WithCronExpression(this CronEvent activity, string value) => activity.With(x => x.CronExpression, new CodeExpression<string>(value));
    }
}