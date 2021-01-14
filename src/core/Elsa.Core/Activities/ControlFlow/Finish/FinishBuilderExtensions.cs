using System;
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

        public static IActivityBuilder Finish(this IBuilder builder, Func<ActivityExecutionContext, ValueTask<string?>> outcome, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Finish(activity => activity.WithOutcome(outcome), lineNumber, sourceFile);

        public static IActivityBuilder Finish(this IBuilder builder, Func<ActivityExecutionContext, string?> outcome, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Finish(activity => activity.WithOutcome(outcome), lineNumber, sourceFile);

        public static IActivityBuilder Finish(this IBuilder builder, Func<string?> outcome, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Finish(activity => activity.WithOutcome(outcome), lineNumber, sourceFile);

        public static IActivityBuilder Finish(this IBuilder builder, string? outcome, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Finish(activity => activity.WithOutcome(outcome), lineNumber, sourceFile);
    }
}