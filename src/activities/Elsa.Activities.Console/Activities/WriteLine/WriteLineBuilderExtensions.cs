using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Console
{
    public static class WriteLineBuilderExtensions
    {
        public static IOutcomeBuilder WriteLine(this IBuilder builder, Action<ISetupActivity<WriteLine>> setup, string? name = default, string? id = default) => WriteLine(builder.Then(setup).WithName(name).WithId(id));
        public static IOutcomeBuilder WriteLine(this IBuilder builder, Func<ActivityExecutionContext, string> text, string? name = default, string? id = default) => builder.WriteLine(activity => activity.Set(x => x.Text, text), name, id);
        public static IOutcomeBuilder WriteLine(this IBuilder builder, Func<ActivityExecutionContext, ValueTask<string>> text, string? name = default, string? id = default) => builder.WriteLine(activity => activity.Set(x => x.Text, text!), name, id);
        public static IOutcomeBuilder WriteLine(this IBuilder builder, Func<string> text, string? name = default, string? id = default) => builder.WriteLine(activity => activity.Set(x => x.Text, text), name, id);
        public static IOutcomeBuilder WriteLine(this IBuilder builder, Func<ValueTask<string>> text, string? name = default, string? id = default) => builder.WriteLine(activity => activity.Set(x => x.Text, text!), name, id);
        public static IOutcomeBuilder WriteLine(this IBuilder builder, string text, string? name = default, string? id = default) => builder.WriteLine(activity => activity.Set(x => x.Text, text), name, id);
        private static IOutcomeBuilder WriteLine(IActivityBuilder writeLine) => writeLine.When(OutcomeNames.Done);
    }
}