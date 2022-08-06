using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class IfBuilderExtensions
    {
        public static IActivityBuilder If(
            this IBuilder builder,
            Action<ISetupActivity<If>>? setup = default,
            Action<IActivityBuilder>? activity = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default)
        {
            var activityBuilder = builder.Then(setup, null, lineNumber, sourceFile);
            activity?.Invoke(activityBuilder);
            return activityBuilder;
        }

        public static IActivityBuilder If(
            this IBuilder builder,
            Func<ActivityExecutionContext, bool> condition,
            Action<IActivityBuilder>? activity = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) => builder.If(x => x.WithCondition(condition), activity, lineNumber, sourceFile);

        public static IActivityBuilder If(this IBuilder builder, Func<bool> condition, Action<IActivityBuilder>? activity = default, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.If(x => x.WithCondition(condition), activity, lineNumber, sourceFile);

        public static IActivityBuilder If(this IBuilder builder, bool condition, Action<IActivityBuilder>? activity = default, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.If(x => x.WithCondition(condition), activity, lineNumber, sourceFile);

        public static IActivityBuilder IfTrue(
            this IBuilder builder,
            Func<ActivityExecutionContext, ValueTask<bool>> condition,
            Action<IOutcomeBuilder> whenTrue,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.If(x => x.WithCondition(condition), @if => whenTrue(@if.When(OutcomeNames.True)), lineNumber, sourceFile);

        public static IActivityBuilder IfTrue(
            this IBuilder builder,
            Func<ActivityExecutionContext, bool> condition,
            Action<IOutcomeBuilder> whenTrue,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.If(x => x.WithCondition(condition), @if => whenTrue(@if.When(OutcomeNames.True)), lineNumber, sourceFile);

        public static IActivityBuilder IfTrue(
            this IBuilder builder,
            Func<bool> condition,
            Action<IOutcomeBuilder> whenTrue,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.If(x => x.WithCondition(condition), @if => whenTrue(@if.When(OutcomeNames.True)), lineNumber, sourceFile);

        public static IActivityBuilder IfTrue(
            this IBuilder builder,
            bool condition,
            Action<IOutcomeBuilder> whenTrue,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.If(x => x.WithCondition(condition), @if => whenTrue(@if.When(OutcomeNames.True)), lineNumber, sourceFile);

        public static IActivityBuilder IfFalse(
            this IBuilder builder,
            Func<ActivityExecutionContext, ValueTask<bool>> condition,
            Action<IOutcomeBuilder> whenFalse,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.If(x => x.WithCondition(condition), @if => whenFalse(@if.When(OutcomeNames.False)), lineNumber, sourceFile);

        public static IActivityBuilder IfFalse(
            this IBuilder builder,
            Func<ActivityExecutionContext, bool> condition,
            Action<IOutcomeBuilder> whenFalse,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.If(x => x.WithCondition(condition), @if => whenFalse(@if.When(OutcomeNames.False)), lineNumber, sourceFile);

        public static IActivityBuilder IfFalse(
            this IBuilder builder,
            Func<bool> condition,
            Action<IOutcomeBuilder> whenFalse,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.If(x => x.WithCondition(condition), @if => whenFalse(@if.When(OutcomeNames.False)), lineNumber, sourceFile);
        
        public static IActivityBuilder IfFalse(
            this IBuilder builder,
            bool condition,
            Action<IOutcomeBuilder> whenFalse,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.If(x => x.WithCondition(condition), @if => whenFalse(@if.When(OutcomeNames.False)), lineNumber, sourceFile);

        public static IActivityBuilder If(
            this IBuilder builder,
            Func<ActivityExecutionContext, bool> condition,
            Action<IOutcomeBuilder> whenTrue,
            Action<IOutcomeBuilder> whenFalse,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.If(x => x.WithCondition(condition), @if =>
            {
                whenTrue(@if.When(OutcomeNames.True));
                whenFalse(@if.When(OutcomeNames.False));
            }, lineNumber, sourceFile);
        
        public static IActivityBuilder If(
            this IBuilder builder,
            Func<bool> condition,
            Action<IOutcomeBuilder> whenTrue,
            Action<IOutcomeBuilder> whenFalse,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.If(x => x.WithCondition(condition), @if =>
            {
                whenTrue(@if.When(OutcomeNames.True));
                whenFalse(@if.When(OutcomeNames.False));
            }, lineNumber, sourceFile);
        
        public static IActivityBuilder If(
            this IBuilder builder,
            Func<ActivityExecutionContext, ValueTask<bool>> condition,
            Action<IOutcomeBuilder> whenTrue,
            Action<IOutcomeBuilder> whenFalse,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.If(x => x.WithCondition(condition), @if =>
            {
                whenTrue(@if.When(OutcomeNames.True));
                whenFalse(@if.When(OutcomeNames.False));
            }, lineNumber, sourceFile);
    }
}