using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Elsa.Activities.Primitives;
using Elsa.ActivityResults;
using Elsa.Services.Models;

// ReSharper disable ExplicitCallerInfoArgument
namespace Elsa.Builders
{
    public static class OutcomeBuilderExtensions
    {
        public static IActivityBuilder Then(
            this IOutcomeBuilder outcomeBuilder,
            Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>> activity,
            Action<IActivityBuilder>? branch = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            outcomeBuilder.Then<Inline>(inline => inline.Set(x => x.Function, activity), branch, lineNumber, sourceFile);

        public static IActivityBuilder Then(
            this IOutcomeBuilder outcomeBuilder,
            Func<ActivityExecutionContext, ValueTask> activity,
            Action<IActivityBuilder>? branch = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) => outcomeBuilder.Then<Inline>(
            inline => inline.Set(x => x.Function, RunInline(activity)), branch, lineNumber, sourceFile);

        public static IActivityBuilder Then(
            this IOutcomeBuilder outcomeBuilder,
            Action<ActivityExecutionContext> activity,
            Action<IActivityBuilder>? branch = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) => outcomeBuilder.Then<Inline>(
            inline => inline.Set(x => x.Function, RunInline(activity)), branch, lineNumber, sourceFile);

        public static IActivityBuilder Then(
            this IOutcomeBuilder outcomeBuilder,
            Action activity,
            Action<IActivityBuilder>? branch = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) => outcomeBuilder.Then<Inline>(
            inline => inline.Set(x => x.Function, RunInline(activity)), branch, lineNumber, sourceFile);

        private static Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>> RunInline(
            Func<ActivityExecutionContext, ValueTask> activity) =>
            async context =>
            {
                await activity(context);
                return new DoneResult();
            };

        private static Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>> RunInline(
            Action<ActivityExecutionContext> activity) =>
            context =>
            {
                activity(context);
                return new ValueTask<IActivityExecutionResult>(new DoneResult());
            };

        private static Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>> RunInline(Action activity) =>
            _ =>
            {
                activity();
                return new ValueTask<IActivityExecutionResult>(new DoneResult());
            };
    }
}