using System;
using System.Collections.Generic;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class SwitchExtensions
    {
        public static ISetupActivity<Switch> WithCases(this ISetupActivity<Switch> activity, Func<ActivityExecutionContext, IEnumerable<string>> value) => activity.Set(x => x.Cases, value);
        public static ISetupActivity<Switch> WithCases(this ISetupActivity<Switch> activity, Func<IEnumerable<string>> value) => activity.Set(x => x.Cases, value);
        public static ISetupActivity<Switch> WithCases(this ISetupActivity<Switch> activity, IEnumerable<string> value) => activity.Set(x => x.Cases, value);
        public static ISetupActivity<Switch> WithCases(this ISetupActivity<Switch> activity, params string[] value) => activity.Set(x => x.Cases, new HashSet<string>(value));
        
        public static ISetupActivity<Switch> WithValue(this ISetupActivity<Switch> activity, Func<ActivityExecutionContext, string> value) => activity.Set(x => x.Value, value);
        public static ISetupActivity<Switch> WithValue(this ISetupActivity<Switch> activity, Func<string> value) => activity.Set(x => x.Value, value);
        public static ISetupActivity<Switch> WithValue(this ISetupActivity<Switch> activity, string value) => activity.Set(x => x.Value, value);
    }
}