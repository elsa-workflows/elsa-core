using System;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Signaling
{
    public static class TriggerSignalBuilderExtensions
    {
        public static ActivityBuilder TriggerSignal(this IBuilder builder, IWorkflowExpression<string> signal, IWorkflowExpression<string>? correlationId = default, IWorkflowExpression? input = default) => builder.Then<TriggerSignal>(x => x.WithSignal(signal).WithCorrelationId(correlationId).WithInput(input));
        public static ActivityBuilder TriggerSignal(this IBuilder builder, Func<ActivityExecutionContext, string> signal, Func<ActivityExecutionContext, string>? correlationId = default, Func<ActivityExecutionContext, object>? input = default) => builder.Then<TriggerSignal>(x => x.WithSignal(signal).WithCorrelationId(correlationId).WithInput(input));
        public static ActivityBuilder TriggerSignal(this IBuilder builder, Func<string> signal, Func<string>? correlationId = default, Func<object>? input = default) => builder.Then<TriggerSignal>(x => x.WithSignal(signal).WithCorrelationId(correlationId).WithInput(input));
        public static ActivityBuilder TriggerSignal(this IBuilder builder, string signal, string? correlationId = default, object? input = default) => builder.Then<TriggerSignal>(x => x.WithSignal(signal).WithCorrelationId(correlationId).WithInput(input));
    }
}