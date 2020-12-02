using System;
using System.Threading.Tasks;
using Elsa.Activities.Primitives;
using Elsa.ActivityResults;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public static class CompositeActivityBuilderExtensions
    {
        public static IActivityBuilder StartWith(this ICompositeActivityBuilder builder, Action action) => builder.StartWith<Inline>(inline => inline.Set(x => x.Function, RunInline(action)));

        public static IActivityBuilder
            StartWith(this ICompositeActivityBuilder builder, Action<ActivityExecutionContext> action) =>
            builder.StartWith<Inline>(inline => inline.Set(x => x.Function, RunInline(action)));

        public static IActivityBuilder StartWith(
            this ICompositeActivityBuilder builder,
            Func<ActivityExecutionContext, ValueTask> action) =>
            builder.StartWith<Inline>(inline => inline.Set(x => x.Function, RunInline(action)));

        public static IActivityBuilder StartWith(
            this ICompositeActivityBuilder builder,
            Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>> action) =>
            builder.StartWith<Inline>(inline => inline.Set(x => x.Function, RunInline(action)));

        public static IActivityBuilder SetVariable(this ICompositeActivityBuilder builder, string variableName, object? value) =>
            builder.StartWith<SetVariable>(
                activity =>
                {
                    activity.Set(x => x.VariableName, _ => variableName);
                    activity.Set(x => x.Value, _ => value);
                });

        public static IActivityBuilder SetVariable(
            this ICompositeActivityBuilder builder,
            string variableName,
            Func<object?> value) =>
            builder.SetVariable(variableName, value());

        private static Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>> RunInline(Action action) =>
            _ =>
            {
                action();
                return new ValueTask<IActivityExecutionResult>(new DoneResult());
            };

        private static Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>> RunInline(Action<ActivityExecutionContext> action) =>
            context =>
            {
                action(context);
                return new ValueTask<IActivityExecutionResult>(new DoneResult());
            };

        private static Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>> RunInline(Func<ActivityExecutionContext, ValueTask> action) =>
            async context =>
            {
                await action(context);
                return new DoneResult();
            };

        private static Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>> RunInline(Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>> action) => async context => await action(context);
    }
}