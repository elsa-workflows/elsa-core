using System;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class IfElseBuilderExtensions
    {
        public static IActivityBuilder IfElse(this IBuilder builder, Action<ISetupActivity<IfElse>>? setup = default, Action<IActivityBuilder>? activity = default)
        {
            var activityBuilder = builder.Then(setup);
            activity?.Invoke(activityBuilder);
            return activityBuilder;
        }
        
        public static IActivityBuilder IfElse(this IBuilder builder, Func<ActivityExecutionContext, bool> condition, Action<IActivityBuilder>? activity = default) => builder.IfElse(x => x.WithCondition(condition), activity);
        public static IActivityBuilder IfElse(this IBuilder builder, Func<bool> condition, Action<IActivityBuilder>? activity = default) => builder.IfElse(x => x.WithCondition(condition), activity);
        public static IActivityBuilder IfElse(this IBuilder builder, bool condition, Action<IActivityBuilder>? activity = default) => builder.IfElse(x => x.WithCondition(condition), activity);
    }
}