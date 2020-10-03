using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Console
{
    public static class WriteLineBuilderExtensions
    {
        public static IOutcomeBuilder WriteLine(this IBuilder builder,
            Action<ISetupActivity<WriteLine>> setup) => WriteLine(builder.Then(setup));

        public static IOutcomeBuilder WriteLine(this IBuilder builder, Func<ActivityExecutionContext, string> text) =>
            builder.WriteLine(activity => activity.Set(x => x.Text, text));

        public static IOutcomeBuilder WriteLine(this IBuilder builder,
            Func<ActivityExecutionContext, ValueTask<string>> text) =>
            builder.WriteLine(activity => activity.Set(x => x.Text, text));

        public static IOutcomeBuilder WriteLine(this IBuilder builder, Func<string> text) =>
            builder.WriteLine(activity => activity.Set(x => x.Text, text));

        public static IOutcomeBuilder WriteLine(this IBuilder builder, Func<ValueTask<string>> text) =>
            builder.WriteLine(activity => activity.Set(x => x.Text, text));

        public static IOutcomeBuilder WriteLine(this IBuilder builder, string text) =>
            builder.WriteLine(activity => activity.Set(x => x.Text, text));

        private static IOutcomeBuilder WriteLine(IActivityBuilder writeLine) => writeLine.When(OutcomeNames.Done);
    }
}