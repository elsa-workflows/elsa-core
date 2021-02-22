using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class IfExtensions
    {
        public static ISetupActivity<If> WithCondition(this ISetupActivity<If> activity, Func<ActivityExecutionContext, ValueTask<bool>> value) => activity.Set(x => x.Condition, value);
        public static ISetupActivity<If> WithCondition(this ISetupActivity<If> activity, Func<ActivityExecutionContext, bool> value) => activity.Set(x => x.Condition, value);
        public static ISetupActivity<If> WithCondition(this ISetupActivity<If> activity, Func<bool> value) => activity.Set(x => x.Condition, value);
        public static ISetupActivity<If> WithCondition(this ISetupActivity<If> activity, bool value) => activity.Set(x => x.Condition, value);
    }
}