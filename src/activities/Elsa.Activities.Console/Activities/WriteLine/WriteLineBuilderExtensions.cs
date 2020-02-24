using System;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Console
{
    public static class WriteLineBuilderExtensions
    {
        public static OutcomeBuilder WriteLine(this IBuilder builder, Action<WriteLine> setup) => WriteLine(builder.Then(setup));
        public static OutcomeBuilder WriteLine(this IBuilder builder, IWorkflowExpression<string> text) => builder.WriteLine(a => a.WithText(text));
        public static OutcomeBuilder WriteLine(this IBuilder builder, Func<ActivityExecutionContext, string> text) => builder.WriteLine(new CodeExpression<string>(text));
        public static OutcomeBuilder WriteLine(this IBuilder builder, Func<string> text) => builder.WriteLine(new CodeExpression<string>(text));
        public static OutcomeBuilder WriteLine(this IBuilder builder, string text) => builder.WriteLine(new CodeExpression<string>(text));
        private static OutcomeBuilder WriteLine(ActivityBuilder writeLine) => writeLine.When(OutcomeNames.Done);
    }
}