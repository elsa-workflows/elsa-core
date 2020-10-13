using System;
using System.Threading.Tasks;
using Elsa.Activities.Primitives;
using Elsa.ActivityResults;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public static class WorkflowBuilderExtensions
    {
        public static IActivityBuilder StartWith(this IWorkflowBuilder builder, Action action) =>
            builder.StartWith<Inline>(x => x.Function = RunInline(action));

        public static IActivityBuilder
            StartWith(this IWorkflowBuilder builder, Action<ActivityExecutionContext> action) =>
            builder.StartWith<Inline>(x => x.Function = RunInline(action));

        public static IActivityBuilder StartWith(
            this IWorkflowBuilder builder,
            Func<ActivityExecutionContext, ValueTask> action) =>
            builder.StartWith<Inline>(x => x.Function = RunInline(action));

        public static IActivityBuilder StartWith(
            this IWorkflowBuilder builder,
            Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>> action) =>
            builder.StartWith<Inline>(x => x.Function = RunInline(action));

        public static IActivityBuilder SetVariable(this IWorkflowBuilder builder, string variableName, object? value) =>
            builder.StartWith<SetVariable>(
                activity =>
                {
                    activity.Set(x => x.VariableName, _ => variableName);
                    activity.Set(x => x.Value, _ => value);
                });

        public static IActivityBuilder SetVariable(
            this IWorkflowBuilder builder,
            string variableName,
            Func<object?> value) => builder.SetVariable(variableName, value());

        private static Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>> RunInline(Action action) =>
            context =>
            {
                action();
                return new ValueTask<IActivityExecutionResult>(new DoneResult());
            };

        private static Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>> RunInline(
            Action<ActivityExecutionContext> action) =>
            context =>
            {
                action(context);
                return new ValueTask<IActivityExecutionResult>(new DoneResult());
            };

        private static Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>> RunInline(
            Func<ActivityExecutionContext, ValueTask> action) =>
            async context =>
            {
                await action(context);
                return new DoneResult();
            };

        private static Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>> RunInline(
            Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>> action) =>
            async context => await action(context);
    }
}