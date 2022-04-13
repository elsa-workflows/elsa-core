using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class ForEachExtensions
    {
        public static ISetupActivity<ForEach> WithItems(this ISetupActivity<ForEach> activity, Func<ActivityExecutionContext, ValueTask<ICollection<object>>> value) => activity.Set(x => x.Items, value!);
        public static ISetupActivity<ForEach> WithItems(this ISetupActivity<ForEach> activity, Func<ActivityExecutionContext, ICollection<object>> value) => activity.Set(x => x.Items, value);
        public static ISetupActivity<ForEach> WithItems(this ISetupActivity<ForEach> activity, Func<ICollection<object>> value) => activity.Set(x => x.Items, value);
        public static ISetupActivity<ForEach> WithItems(this ISetupActivity<ForEach> activity, ICollection<object> value) => activity.Set(x => x.Items, value);
    }
}