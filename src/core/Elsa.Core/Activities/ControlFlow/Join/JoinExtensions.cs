using System;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class JoinExtensions
    {
        public static ISetupActivity<Join> WithMode(this ISetupActivity<Join> activity, Func<ActivityExecutionContext, Join.JoinMode> value) => activity.Set(x => x.Mode, value);
        public static ISetupActivity<Join> WithMode(this ISetupActivity<Join> activity, Func<Join.JoinMode> value) => activity.Set(x => x.Mode, value);
        public static ISetupActivity<Join> WithMode(this ISetupActivity<Join> activity, Join.JoinMode value) => activity.Set(x => x.Mode, value);
        public static ISetupActivity<Join> WithEagerJoin(this ISetupActivity<Join> activity, Func<ActivityExecutionContext, bool> value) => activity.Set(x => x.EagerJoin, value);
        public static ISetupActivity<Join> WithEagerJoin(this ISetupActivity<Join> activity, Func<bool> value) => activity.Set(x => x.EagerJoin, value);
        public static ISetupActivity<Join> WithEagerJoin(this ISetupActivity<Join> activity, bool value) => activity.Set(x => x.EagerJoin, value);
    }
}