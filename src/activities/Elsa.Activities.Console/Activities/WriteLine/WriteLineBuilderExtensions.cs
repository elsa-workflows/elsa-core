using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Console
{
    public static class WriteLineBuilderExtensions
    {
        public static IActivityBuilder WriteLine(this IBuilder builder, Action<ISetupActivity<WriteLine>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder WriteLine(this IBuilder builder, Func<ActivityExecutionContext, string> text, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.WriteLine(activity => activity.WithText(text), lineNumber, sourceFile);

        public static IActivityBuilder WriteLine(this IBuilder builder, Func<ActivityExecutionContext, ValueTask<string>> text, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.WriteLine(activity => activity.WithText(text!), lineNumber, sourceFile);

        public static IActivityBuilder WriteLine(this IBuilder builder, Func<string> text, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.WriteLine(activity => activity.WithText(text!), lineNumber, sourceFile);

        public static IActivityBuilder WriteLine(this IBuilder builder, Func<ValueTask<string>> text, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.WriteLine(activity => activity.WithText(text!), lineNumber, sourceFile);

        public static IActivityBuilder WriteLine(this IBuilder builder, string text, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.WriteLine(activity => activity.WithText(text!), lineNumber, sourceFile);
    }
}