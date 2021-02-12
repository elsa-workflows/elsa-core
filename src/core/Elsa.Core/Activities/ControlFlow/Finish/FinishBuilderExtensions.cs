using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class FinishBuilderExtensions
    {
        public static IActivityBuilder Finish(this IBuilder builder, Action<ISetupActivity<Finish>>? setup = default, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder Finish(this IBuilder builder, Func<ActivityExecutionContext, ValueTask<object?>> output, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Finish(activity => activity.WithOutput(output), lineNumber, sourceFile);

        public static IActivityBuilder Finish(this IBuilder builder, Func<ActivityExecutionContext, object?> output, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Finish(activity => activity.WithOutput(output), lineNumber, sourceFile);

        public static IActivityBuilder Finish(this IBuilder builder, Func<object?> output, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Finish(activity => activity.WithOutput(output), lineNumber, sourceFile);

        public static IActivityBuilder Finish(this IBuilder builder, object? output, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Finish(activity => activity.WithOutput(output), lineNumber, sourceFile);

        public static IActivityBuilder Finish(this IBuilder builder, Func<ActivityExecutionContext, ValueTask<IEnumerable<string>?>> outcomes, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Finish(activity => activity.WithOutcome(outcomes), lineNumber, sourceFile);

        public static IActivityBuilder Finish(this IBuilder builder, Func<ActivityExecutionContext, IEnumerable<string>> outcomes, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Finish(activity => activity.WithOutcome(outcomes), lineNumber, sourceFile);

        public static IActivityBuilder Finish(this IBuilder builder, Func<IEnumerable<string>> outcomes, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Finish(activity => activity.WithOutcome(outcomes), lineNumber, sourceFile);

        public static IActivityBuilder Finish(this IBuilder builder, IEnumerable<string> outcomes, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Finish(activity => activity.WithOutcome(outcomes), lineNumber, sourceFile);
    }
}