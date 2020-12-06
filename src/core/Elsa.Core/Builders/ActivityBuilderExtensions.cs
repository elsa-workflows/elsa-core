using System;
using System.Threading.Tasks;
using Elsa.Activities.Primitives;
using Elsa.ActivityResults;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public static class ActivityBuilderExtensions
    {
        public static IActivityBuilder Then(this IBuilder builder,
            Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>> activity) =>
            builder.Then<Inline>(inline => inline.Set(x => x.Function, RunInline(activity)));

        public static IActivityBuilder
            Then(this IBuilder builder, Func<ActivityExecutionContext, ValueTask> activity) =>
            builder.Then<Inline>(inline => inline.Set(x => x.Function, RunInline(activity)));
        
        public static IActivityBuilder Then(this IBuilder builder, Action<ActivityExecutionContext> activity) =>
            builder.Then<Inline>(inline => inline.Set(x => x.Function, RunInline(activity)));
        
        public static IActivityBuilder Then(this IBuilder builder, Action activity) =>
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
                return new ValueTask<IActivityExecutionResult>(new OutcomeResult());
            };
    }
}