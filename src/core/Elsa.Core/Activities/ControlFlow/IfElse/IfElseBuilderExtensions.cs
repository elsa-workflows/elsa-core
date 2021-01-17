using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class IfElseBuilderExtensions
    {
        public static IActivityBuilder IfElse(
            this IBuilder builder,
            Action<ISetupActivity<IfElse>>? setup = default,
            Action<IActivityBuilder>? activity = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default)
        {
            var activityBuilder = builder.Then(setup, null, lineNumber, sourceFile);
            activity?.Invoke(activityBuilder);
            return activityBuilder;
        }

        public static IActivityBuilder IfElse(
            this IBuilder builder,
            Func<ActivityExecutionContext, bool> condition,
            Action<IActivityBuilder>? activity = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) => builder.IfElse(x => x.WithCondition(condition), activity, lineNumber, sourceFile);

        public static IActivityBuilder IfElse(this IBuilder builder, Func<bool> condition, Action<IActivityBuilder>? activity = default, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.IfElse(x => x.WithCondition(condition), activity, lineNumber, sourceFile);

        public static IActivityBuilder IfElse(this IBuilder builder, bool condition, Action<IActivityBuilder>? activity = default, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.IfElse(x => x.WithCondition(condition), activity, lineNumber, sourceFile);

        public static IActivityBuilder IfTrue(
            this IBuilder builder,
            Func<ActivityExecutionContext, ValueTask<bool>> condition,
            Action<IOutcomeBuilder> whenTrue,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.IfElse(x => x.WithCondition(condition), ifElse => whenTrue(ifElse.When(OutcomeNames.True)), lineNumber, sourceFile);

        public static IActivityBuilder IfTrue(
            this IBuilder builder,
            Func<ActivityExecutionContext, bool> condition,
            Action<IOutcomeBuilder> whenTrue,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.IfElse(x => x.WithCondition(condition), ifElse => whenTrue(ifElse.When(OutcomeNames.True)), lineNumber, sourceFile);

        public static IActivityBuilder IfTrue(
            this IBuilder builder,
            Func<bool> condition,
            Action<IOutcomeBuilder> whenTrue,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.IfElse(x => x.WithCondition(condition), ifElse => whenTrue(ifElse.When(OutcomeNames.True)), lineNumber, sourceFile);

        public static IActivityBuilder IfTrue(
            this IBuilder builder,
            bool condition,
            Action<IOutcomeBuilder> whenTrue,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.IfElse(x => x.WithCondition(condition), ifElse => whenTrue(ifElse.When(OutcomeNames.True)), lineNumber, sourceFile);

        public static IActivityBuilder IfFalse(
            this IBuilder builder,
            Func<ActivityExecutionContext, ValueTask<bool>> condition,
            Action<IOutcomeBuilder> whenFalse,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.IfElse(x => x.WithCondition(condition), ifElse => whenFalse(ifElse.When(OutcomeNames.False)), lineNumber, sourceFile);

        public static IActivityBuilder IfFalse(
            this IBuilder builder,
            Func<ActivityExecutionContext, bool> condition,
            Action<IOutcomeBuilder> whenFalse,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.IfElse(x => x.WithCondition(condition), ifElse => whenFalse(ifElse.When(OutcomeNames.False)), lineNumber, sourceFile);

        public static IActivityBuilder IfFalse(
            this IBuilder builder,
            Func<bool> condition,
            Action<IOutcomeBuilder> whenFalse,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.IfElse(x => x.WithCondition(condition), ifElse => whenFalse(ifElse.When(OutcomeNames.False)), lineNumber, sourceFile);
        
        public static IActivityBuilder IfFalse(
            this IBuilder builder,
            bool condition,
            Action<IOutcomeBuilder> whenFalse,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.IfElse(x => x.WithCondition(condition), ifElse => whenFalse(ifElse.When(OutcomeNames.False)), lineNumber, sourceFile);

        public static IActivityBuilder IfElse(
            this IBuilder builder,
            Func<ActivityExecutionContext, bool> condition,
            Action<IOutcomeBuilder> whenTrue,
            Action<IOutcomeBuilder> whenFalse,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.IfElse(x => x.WithCondition(condition), ifElse =>
            {
                whenTrue(ifElse.When(OutcomeNames.True));
                whenFalse(ifElse.When(OutcomeNames.False));
            }, lineNumber, sourceFile);
    }
}