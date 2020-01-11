using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class SwitchExtensions
    {
        public static Switch WithValue(this Switch activity, IWorkflowExpression<string> value) => activity.With(x => x.Value, value);
        public static Switch WithValue(this Switch activity, Func<ActivityExecutionContext, string> value) => activity.With(x => x.Value, new CodeExpression<string>(value));
        public static Switch WithValue(this Switch activity, Func<string> value) => activity.With(x => x.Value, new CodeExpression<string>(value));
        public static Switch WithValue(this Switch activity, string value) => activity.With(x => x.Value, new CodeExpression<string>(value));
        public static Switch WithCases(this Switch activity, IEnumerable<string> value) => activity.With(x => x.Cases, new HashSet<string>(value));
        public static Switch WithCases(this Switch activity, string[] value) => activity.With(x => x.Cases, new HashSet<string>(value));
    }
}