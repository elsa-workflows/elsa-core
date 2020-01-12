using System;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Signaling
{
    public static class TriggerSignalExtensions
    {
        public static TriggerSignal WithSignal(this TriggerSignal activity, IWorkflowExpression<string> value) => activity.With(x => x.Signal, value);
        public static TriggerSignal WithSignal(this TriggerSignal activity, Func<ActivityExecutionContext, string> value) => activity.With(x => x.Signal, new CodeExpression<string>(value));
        public static TriggerSignal WithSignal(this TriggerSignal activity, Func<string> value) => activity.With(x => x.Signal, new CodeExpression<string>(value));
        public static TriggerSignal WithSignal(this TriggerSignal activity, string value) => activity.With(x => x.Signal, new CodeExpression<string>(value));
        
        public static TriggerSignal WithCorrelationId(this TriggerSignal activity, IWorkflowExpression<string> value) => activity.With(x => x.CorrelationId, value);
        public static TriggerSignal WithCorrelationId(this TriggerSignal activity, Func<ActivityExecutionContext, string> value) => activity.With(x => x.CorrelationId, new CodeExpression<string>(value));
        public static TriggerSignal WithCorrelationId(this TriggerSignal activity, Func<string> value) => activity.With(x => x.CorrelationId, new CodeExpression<string>(value));
        public static TriggerSignal WithCorrelationId(this TriggerSignal activity, string value) => activity.With(x => x.CorrelationId, new CodeExpression<string>(value));

        public static TriggerSignal WithInput(this TriggerSignal activity, IWorkflowExpression? value) => activity.With(x => x.Input, value);
        public static TriggerSignal WithInput<T>(this TriggerSignal activity, Func<ActivityExecutionContext, T> value) => activity.With(x => x.Input, new CodeExpression<T>(value));
        public static TriggerSignal WithInput<T>(this TriggerSignal activity, Func<T> value) => activity.With(x => x.Input, new CodeExpression<T>(value));
        public static TriggerSignal WithInput<T>(this TriggerSignal activity, T value) => activity.With(x => x.Input, new CodeExpression<T>(value));
    }
}