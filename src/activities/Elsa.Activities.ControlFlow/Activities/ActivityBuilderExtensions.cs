using System;
using Elsa.Services;

namespace Elsa.Activities.ControlFlow.Activities
{
    public static class ActivityBuilderExtensions
    {
        public static IActivityBuilder Fork(this IActivityBuilder activityBuilder, Action<Fork> setup = null, Action<IActivityBuilder> branch = null, string name = default)
        {
            return activityBuilder.Then(setup, branch, name);
        }

        public static IActivityBuilder ForEach(this IActivityBuilder activityBuilder, Action<ForEach> setup = null, Action<IActivityBuilder> branch = null, string name = default)
        {
            return activityBuilder.Then(setup, branch, name);
        }

        public static IActivityBuilder While(this IActivityBuilder activityBuilder, Action<While> setup = null, Action<IActivityBuilder> branch = null, string name = default)
        {
            return activityBuilder.Then(setup, branch, name);
        }

        public static IActivityBuilder Join(this IActivityBuilder activityBuilder, Action<Join> setup = null, Action<IActivityBuilder> branch = null, string name = default)
        {
            return activityBuilder.Then(setup, branch, name);
        }

        public static IActivityBuilder IfElse(this IActivityBuilder activityBuilder, Action<IfElse> setup = null, Action<IActivityBuilder> branch = null, string name = default)
        {
            return activityBuilder.Then(setup, branch, name);
        }

        public static IActivityBuilder Switch(this IActivityBuilder activityBuilder, Action<Switch> setup = null, Action<IActivityBuilder> branch = null, string name = default)
        {
            return activityBuilder.Then(setup, branch, name);
        }
    }
}