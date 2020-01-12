using System;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Signaling
{
    public static class TriggerEventBuilderExtensions
    {
        public static ActivityBuilder TriggerEvent(this IBuilder builder, IWorkflowExpression<string> activityType, IWorkflowExpression<string>? correlationId = default, IWorkflowExpression? input = default) => builder.Then<TriggerEvent>(x => x.WithActivityType(activityType).WithCorrelationId(correlationId).WithInput(input));
        public static ActivityBuilder TriggerEvent(this IBuilder builder, Func<ActivityExecutionContext, string> activityType, Func<ActivityExecutionContext, string>? correlationId = default, Func<ActivityExecutionContext, object>? input = default) => builder.Then<TriggerEvent>(x => x.WithActivityType(activityType).WithCorrelationId(correlationId).WithInput(input));
        public static ActivityBuilder TriggerEvent(this IBuilder builder, Func<string> activityType, Func<string>? correlationId = default, Func<object>? input = default) => builder.Then<TriggerEvent>(x => x.WithActivityType(activityType).WithCorrelationId(correlationId).WithInput(input));
        public static ActivityBuilder TriggerEvent(this IBuilder builder, string activityType, string? correlationId = default, object? input = default) => builder.Then<TriggerEvent>(x => x.WithActivityType(activityType).WithCorrelationId(correlationId).WithInput(input));
    }
}