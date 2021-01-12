using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Console
{
    public static class WriteLineBuilderExtensions
    {
        public static IActivityBuilder WriteLine(this IBuilder builder, Action<ISetupActivity<WriteLine>> setup) => builder.Then(setup);
        public static IActivityBuilder WriteLine(this IBuilder builder, Func<ActivityExecutionContext, string> text) => builder.WriteLine(activity => activity.WithText(text));
        public static IActivityBuilder WriteLine(this IBuilder builder, Func<ActivityExecutionContext, ValueTask<string>> text) => builder.WriteLine(activity => activity.WithText(text!));
        public static IActivityBuilder WriteLine(this IBuilder builder, Func<string> text) => builder.WriteLine(activity => activity.WithText(text!));
        public static IActivityBuilder WriteLine(this IBuilder builder, Func<ValueTask<string>> text) => builder.WriteLine(activity => activity.WithText(text!));
        public static IActivityBuilder WriteLine(this IBuilder builder, string text) => builder.WriteLine(activity => activity.WithText(text!));
        
    }
}