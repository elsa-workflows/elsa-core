using System;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Signaling
{
    public static class TriggerEventExtensions
    {
        public static TriggerEvent WithActivityType(this TriggerEvent activity, IWorkflowExpression<string> value) => activity.With(x => x.ActivityType, value);
        public static TriggerEvent WithActivityType(this TriggerEvent activity, Func<ActivityExecutionContext, string> value) => activity.With(x => x.ActivityType, new CodeExpression<string>(value));
        public static TriggerEvent WithActivityType(this TriggerEvent activity, Func<string> value) => activity.With(x => x.ActivityType, new CodeExpression<string>(value));
        public static TriggerEvent WithActivityType(this TriggerEvent activity, string value) => activity.With(x => x.ActivityType, new CodeExpression<string>(value));
        
        public static TriggerEvent WithCorrelationId(this TriggerEvent activity, IWorkflowExpression<string> value) => activity.With(x => x.CorrelationId, value);
        public static TriggerEvent WithCorrelationId(this TriggerEvent activity, Func<ActivityExecutionContext, string> value) => activity.With(x => x.CorrelationId, new CodeExpression<string>(value));
        public static TriggerEvent WithCorrelationId(this TriggerEvent activity, Func<string> value) => activity.With(x => x.CorrelationId, new CodeExpression<string>(value));
        public static TriggerEvent WithCorrelationId(this TriggerEvent activity, string value) => activity.With(x => x.CorrelationId, new CodeExpression<string>(value));

        public static TriggerEvent WithInput(this TriggerEvent activity, IWorkflowExpression? value) => activity.With(x => x.Input, value);
        public static TriggerEvent WithInput<T>(this TriggerEvent activity, Func<ActivityExecutionContext, T> value) => activity.With(x => x.Input, new CodeExpression<T>(value));
        public static TriggerEvent WithInput<T>(this TriggerEvent activity, Func<T> value) => activity.With(x => x.Input, new CodeExpression<T>(value));
        public static TriggerEvent WithInput<T>(this TriggerEvent activity, T value) => activity.With(x => x.Input, new CodeExpression<T>(value));
    }
}