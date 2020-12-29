using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class IfThenExtensions
    {
        public static ISetupActivity<IfThen> WithConditions(this ISetupActivity<IfThen> activity, Func<ActivityExecutionContext, ValueTask<ICollection<IfThenCondition>>> value) => activity.Set(x => x.Conditions, value!);
        public static ISetupActivity<IfThen> WithConditions(this ISetupActivity<IfThen> activity, Func<ActivityExecutionContext, ICollection<IfThenCondition>> value) => activity.Set(x => x.Conditions, value!);
        public static ISetupActivity<IfThen> WithConditions(this ISetupActivity<IfThen> activity, Func<ICollection<IfThenCondition>> value) => activity.Set(x => x.Conditions, value!);
        public static ISetupActivity<IfThen> WithConditions(this ISetupActivity<IfThen> activity, ICollection<IfThenCondition> value) => activity.Set(x => x.Conditions, value!);
    }
}