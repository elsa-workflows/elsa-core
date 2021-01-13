using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class IfElseExtensions
    {
        public static ISetupActivity<IfElse> WithCondition(this ISetupActivity<IfElse> activity, Func<ActivityExecutionContext, ValueTask<bool>> value) => activity.Set(x => x.Condition, value);
        public static ISetupActivity<IfElse> WithCondition(this ISetupActivity<IfElse> activity, Func<ActivityExecutionContext, bool> value) => activity.Set(x => x.Condition, value);
        public static ISetupActivity<IfElse> WithCondition(this ISetupActivity<IfElse> activity, Func<bool> value) => activity.Set(x => x.Condition, value);
        public static ISetupActivity<IfElse> WithCondition(this ISetupActivity<IfElse> activity, bool value) => activity.Set(x => x.Condition, value);
    }
}