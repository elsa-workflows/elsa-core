using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Elsa.Activities.Primitives;
using Elsa.ActivityResults;
using Elsa.Services.Models;

// ReSharper disable ExplicitCallerInfoArgument
namespace Elsa.Builders
{
    public static class InlineActivityBuilderExtensions
    {
        public static IActivityBuilder Then(
            this IBuilder builder,
            Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>> activity,
            Action<IActivityBuilder>? branch = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.Then<Inline>(inline => inline.Set(x => x.Function, RunInline(activity)), branch, lineNumber, sourceFile);

        public static IActivityBuilder
            Then(this IBuilder builder, Func<ActivityExecutionContext, ValueTask> activity, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then<Inline>(inline => inline.Set(x => x.Function, RunInline(activity)), null, lineNumber, sourceFile);

        public static IActivityBuilder Then(this IBuilder builder, Action<ActivityExecutionContext> activity, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then<Inline>(inline => inline.Set(x => x.Function, RunInline(activity)), null, lineNumber, sourceFile);

        public static IActivityBuilder Then(this IBuilder builder, Action activity) =>
            builder.Then<Inline>(inline => inline.Set(x => x.Function, RunInline(activity)));
        
        public static IActivityBuilder Then(this IBuilder builder, Func<Task> activity) =>
            builder.Then<Inline>(inline => inline.Set(x => x.Function, RunInline(activity)));

        private static Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>> RunInline(
            Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>> activity) =>
            async context => await activity(context);

        private static Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>> RunInline(
            Func<ActivityExecutionContext, ValueTask> activity) =>
            async context =>
            {
                await activity(context);
                return new OutcomeResult();
            };

        private static Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>> RunInline(
            Action<ActivityExecutionContext> activity) =>
            context =>
            {
                activity(context);
                return new ValueTask<IActivityExecutionResult>(new OutcomeResult());
            };

        private static Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>> RunInline(
            Func<ActivityExecutionContext, Task> activity) =>
            async context =>
            {
                await activity(context);
                return new OutcomeResult();
            };

        private static Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>> RunInline(Action activity) =>
            context =>
            {
                activity();
                return new ValueTask<IActivityExecutionResult>(new DoneResult());
            };
        
        private static Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>> RunInline(Func<Task> activity) =>
            async context =>
            {
                await activity();
                return new DoneResult();
            };
    }
}