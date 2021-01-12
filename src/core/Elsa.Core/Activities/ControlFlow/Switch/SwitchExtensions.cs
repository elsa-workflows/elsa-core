using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class SwitchExtensions
    {
        public static ISetupActivity<Switch> WithCases(this ISetupActivity<Switch> activity, Func<ActivityExecutionContext, ValueTask<ICollection<SwitchCase>>> value) => activity.Set(x => x.Cases, value!);
        public static ISetupActivity<Switch> WithCases(this ISetupActivity<Switch> activity, Func<ActivityExecutionContext, ICollection<SwitchCase>> value) => activity.Set(x => x.Cases, value!);
        public static ISetupActivity<Switch> WithCases(this ISetupActivity<Switch> activity, Func<ICollection<SwitchCase>> value) => activity.Set(x => x.Cases, value!);
        public static ISetupActivity<Switch> WithCases(this ISetupActivity<Switch> activity, ICollection<SwitchCase> value) => activity.Set(x => x.Cases, value!);
        public static ISetupActivity<Switch> WithMode(this ISetupActivity<Switch> activity, SwitchMode value) => activity.Set(x => x.Mode, value);
    }
}