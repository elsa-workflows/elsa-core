using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class WhileBuilderExtensions
    {
        public static IActivityBuilder While(this IBuilder builder, Action<ISetupActivity<While>> setup, Action<IOutcomeBuilder> iterate) => builder.Then(setup, branch => iterate(branch.When(OutcomeNames.Iterate)));
        public static IActivityBuilder While(this IBuilder builder, Func<ActivityExecutionContext, ValueTask<bool>> condition, Action<IOutcomeBuilder> iterate) => builder.While(activity => activity.Set(x => x.Condition, condition), iterate);
        public static IActivityBuilder While(this IBuilder builder, Func<ActivityExecutionContext, bool> condition, Action<IOutcomeBuilder> iterate) => builder.While(activity => activity.Set(x => x.Condition, condition), iterate);
        public static IActivityBuilder While(this IBuilder builder, Func<bool> condition, Action<IOutcomeBuilder> iterate) => builder.While(activity => activity.Set(x => x.Condition, condition), iterate);
        public static IActivityBuilder While(this IBuilder builder, bool condition, Action<IOutcomeBuilder> iterate) => builder.While(activity => activity.Set(x => x.Condition, condition), iterate);
    }
}