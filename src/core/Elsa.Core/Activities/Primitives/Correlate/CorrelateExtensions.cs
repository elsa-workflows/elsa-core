using System;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Primitives
{
    public static class CorrelateExtensions
    {
        public static Correlate WithValue(this Correlate activity, IWorkflowExpression<string> value) => activity.With(x => x.Value, value);
        public static Correlate WithValue(this Correlate activity, Func<ActivityExecutionContext, string> value) => activity.With(x => x.Value, new CodeExpression<string>(value));
        public static Correlate WithValue(this Correlate activity, Func<string> value) => activity.With(x => x.Value, new CodeExpression<string>(value));
        public static Correlate WithValue(this Correlate activity, string value) => activity.With(x => x.Value, new CodeExpression<string>(value));
    }
}