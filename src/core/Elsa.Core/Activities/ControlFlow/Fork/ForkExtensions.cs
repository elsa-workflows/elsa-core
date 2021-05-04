using System;
using System.Collections.Generic;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class ForkExtensions
    {
        public static ISetupActivity<Fork> WithBranches(this ISetupActivity<Fork> activity, Func<ActivityExecutionContext, HashSet<string>> value) => activity.Set(x => x.Branches, value);
        public static ISetupActivity<Fork> WithBranches(this ISetupActivity<Fork> activity, Func<HashSet<string>> value) => activity.Set(x => x.Branches, value);
        public static ISetupActivity<Fork> WithBranches(this ISetupActivity<Fork> activity, HashSet<string> value) => activity.Set(x => x.Branches, value);
        public static ISetupActivity<Fork> WithBranches(this ISetupActivity<Fork> activity, params string[] value) => activity.WithBranches(new HashSet<string>(value));
    }
}