using System;
using Elsa.Activities.Timers;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    public static class CronEventBuilderExtensions
    {
        public static ActivityBuilder CronEvent(this IBuilder builder, Action<CronEvent>? setup = default) => builder.Then(setup);
        public static ActivityBuilder CronEvent(this IBuilder builder, IWorkflowExpression<string> cronExpression) => builder.CronEvent(x => x.WithCronExpression(cronExpression));
        public static ActivityBuilder CronEvent(this IBuilder builder, Func<ActivityExecutionContext, string> cronExpression) => builder.CronEvent(x => x.WithCronExpression(cronExpression));
        public static ActivityBuilder CronEvent(this IBuilder builder, Func<string> cronExpression) => builder.CronEvent(x => x.WithCronExpression(cronExpression));
        public static ActivityBuilder CronEvent(this IBuilder builder, string cronExpression) => builder.CronEvent(x => x.WithCronExpression(cronExpression));
    }
}