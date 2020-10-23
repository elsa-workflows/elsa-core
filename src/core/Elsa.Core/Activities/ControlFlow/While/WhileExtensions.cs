using System;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class WhileExtensions
    {
        public static ISetupActivity<While> WithCondition(this ISetupActivity<While> activity, Func<ActivityExecutionContext, bool> value) => activity.Set(x => x.Condition, value);
        public static ISetupActivity<While> WithCondition(this ISetupActivity<While> activity, Func<bool> value) => activity.Set(x => x.Condition, value);
        public static ISetupActivity<While> WithCondition(this ISetupActivity<While> activity, bool value) => activity.Set(x => x.Condition, value);
    }
}