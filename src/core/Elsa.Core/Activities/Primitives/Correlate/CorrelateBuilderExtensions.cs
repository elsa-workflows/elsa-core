using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Elsa.Activities.Primitives;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class CorrelateBuilderExtensions
    {
        public static IActivityBuilder Correlate(this IBuilder builder, Action<ISetupActivity<Correlate>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder Correlate(this IBuilder builder, Func<ActivityExecutionContext, ValueTask<string?>> value, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Correlate(activity => activity.Set(x => x.Value, value), lineNumber, sourceFile);

        public static IActivityBuilder Correlate(this IBuilder builder, Func<ActivityExecutionContext, string?> value, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Correlate(activity => activity.Set(x => x.Value, value), lineNumber, sourceFile);

        public static IActivityBuilder Correlate(this IBuilder builder, Func<ValueTask<string?>> value, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Correlate(activity => activity.Set(x => x.Value, value), lineNumber, sourceFile);

        public static IActivityBuilder Correlate(this IBuilder builder, Func<string?> value, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Correlate(activity => activity.Set(x => x.Value, value), lineNumber, sourceFile);

        public static IActivityBuilder Correlate(this IBuilder builder, string? value, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Correlate(activity => activity.Set(x => x.Value, value), lineNumber, sourceFile);
    }
}