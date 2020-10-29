using System;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class IfElseBuilderExtensions
    {
        public static IActivityBuilder IfElse(this IBuilder builder, Action<ISetupActivity<IfElse>>? setup = default, Action<IActivityBuilder>? ifElse = default)
        {
            var activityBuilder = builder.Then(setup);
            ifElse?.Invoke(activityBuilder);
            return activityBuilder;
        }

        public static IActivityBuilder IfElse(this IBuilder builder, Action<ISetupActivity<IfElse>>? setup = default, string? name = default) => builder.Then(setup).WithName(name);
        public static IActivityBuilder IfElse(this IBuilder builder, Func<ActivityExecutionContext, bool> condition, string? name = default) => builder.IfElse(activity => activity.Set(x => x.Condition, condition), name);
        public static IActivityBuilder IfElse(this IBuilder builder, Func<bool> condition, string? name = default) => builder.IfElse(activity => activity.Set(x => x.Condition, condition), name);
        public static IActivityBuilder IfElse(this IBuilder builder, bool condition, string? name = default) => builder.IfElse(activity => activity.Set(x => x.Condition, condition), name);
    }
}