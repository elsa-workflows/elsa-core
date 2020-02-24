using System;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Console
{
    public static class WriteLineExtensions
    {
        public static WriteLine WithText(this WriteLine activity, IWorkflowExpression<string> value) => activity.With(x => x.Text, value);
        public static WriteLine WithText(this WriteLine activity, Func<ActivityExecutionContext, string> value) => activity.With(x => x.Text, new CodeExpression<string>(value));
        public static WriteLine WithText(this WriteLine activity, Func<string> value) => activity.With(x => x.Text, new CodeExpression<string>(value));
        public static WriteLine WithText(this WriteLine activity, string value) => activity.With(x => x.Text, new CodeExpression<string>(value));
    }
}