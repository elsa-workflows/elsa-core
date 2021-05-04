using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Primitives
{
    public static class SetNameBuilderExtensions
    {
        public static IActivityBuilder SetName(this IBuilder builder, Action<ISetupActivity<SetName>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder SetName(this IBuilder builder, Func<ActivityExecutionContext, ValueTask<string?>> value, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SetName(activity => activity.WithValue(value), lineNumber, sourceFile);

        public static IActivityBuilder SetName(this IBuilder builder, Func<ActivityExecutionContext, string?> value, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SetName(activity => activity.WithValue(value), lineNumber, sourceFile);

        public static IActivityBuilder SetName(this IBuilder builder, Func<ValueTask<string?>> value, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SetName(activity => activity.WithValue(value), lineNumber, sourceFile);

        public static IActivityBuilder SetName(this IBuilder builder, Func<string?> value, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SetName(activity => activity.WithValue(value), lineNumber, sourceFile);

        public static IActivityBuilder SetName(this IBuilder builder, string? value, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SetName(activity => activity.WithValue(value), lineNumber, sourceFile);
    }
}