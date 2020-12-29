using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class IfThenBuilderExtensions
    {
        public static IActivityBuilder IfThen(this IBuilder builder, Action<ISetupActivity<IfThen>>? setup = default, Action<IActivityBuilder>? activity = default)
        {
            var activityBuilder = builder.Then(setup);
            activity?.Invoke(activityBuilder);
            return activityBuilder;
        }

        public static IActivityBuilder IfThen(this IBuilder builder, Func<ActivityExecutionContext, ValueTask<ICollection<IfThenCondition>>> conditions, IfThenMatchMode mode, Action<IActivityBuilder>? activity = default) => builder.IfThen(x => x.WithConditions(conditions).WithMode(mode), activity);
        public static IActivityBuilder IfThen(this IBuilder builder, Func<ActivityExecutionContext, ValueTask<ICollection<IfThenCondition>>> conditions, Action<IActivityBuilder>? activity = default) => builder.IfThen(x => x.WithConditions(conditions), activity);
        public static IActivityBuilder IfThen(this IBuilder builder, Func<ActivityExecutionContext, ICollection<IfThenCondition>> conditions, IfThenMatchMode mode, Action<IActivityBuilder>? activity = default) => builder.IfThen(x => x.WithConditions(conditions).WithMode(mode), activity);
        public static IActivityBuilder IfThen(this IBuilder builder, Func<ActivityExecutionContext, ICollection<IfThenCondition>> conditions, Action<IActivityBuilder>? activity = default) => builder.IfThen(x => x.WithConditions(conditions), activity);
        public static IActivityBuilder IfThen(this IBuilder builder, Func<ICollection<IfThenCondition>> conditions, IfThenMatchMode mode, Action<IActivityBuilder>? activity = default) => builder.IfThen(x => x.WithConditions(conditions).WithMode(mode), activity);
        public static IActivityBuilder IfThen(this IBuilder builder, Func<ICollection<IfThenCondition>> conditions, Action<IActivityBuilder>? activity = default) => builder.IfThen(x => x.WithConditions(conditions), activity);
        public static IActivityBuilder IfThen(this IBuilder builder, ICollection<IfThenCondition> conditions, IfThenMatchMode mode, Action<IActivityBuilder>? activity = default) => builder.IfThen(x => x.WithConditions(conditions).WithMode(mode), activity);
        public static IActivityBuilder IfThen(this IBuilder builder, ICollection<IfThenCondition> conditions, Action<IActivityBuilder>? activity = default) => builder.IfThen(x => x.WithConditions(conditions), activity);

        public static IActivityBuilder IfThen(this IBuilder builder, Action<IfThenConditionBuilder> conditions, Action<IActivityBuilder>? activity = default) => builder.IfThen(conditions, IfThenMatchMode.MatchFirst, activity);

        public static IActivityBuilder IfThen(this IBuilder builder, Action<IfThenConditionBuilder> conditions, IfThenMatchMode mode, Action<IActivityBuilder>? activity = default)
        {
            return builder.IfThen(x =>
            {
                conditions(x);
                return new ValueTask();
            }, mode, activity);
        }

        public static IActivityBuilder IfThen(this IBuilder builder, Func<IfThenConditionBuilder, ValueTask> conditions, Action<IActivityBuilder>? activity = default) => builder.IfThen(conditions, IfThenMatchMode.MatchFirst, activity);

        public static IActivityBuilder IfThen(this IBuilder builder, Func<IfThenConditionBuilder, ValueTask> conditions, IfThenMatchMode mode, Action<IActivityBuilder>? activity = default)
        {
            return builder.IfThen(async context =>
            {
                var conditionBuilder = new IfThenConditionBuilder(context);
                await conditions(conditionBuilder);
                return conditionBuilder.Conditions;
            }, mode, activity);
        }
    }
}