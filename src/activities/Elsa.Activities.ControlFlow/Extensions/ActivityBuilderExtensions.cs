using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow.Activities
{
    public static class ActivityBuilderExtensions
    {
        public static IActivityBuilder Fork(this IActivityBuilder activityBuilder, Action<Fork> setup = null, Action<IActivityBuilder> branch = null, string name = default) 
            => activityBuilder.Then(setup, branch, name);

        public static IActivityBuilder ForEach(this IActivityBuilder activityBuilder, Action<ForEach> setup = null, Action<IActivityBuilder> branch = null, string name = default) 
            => activityBuilder.Then(setup, branch, name);

        public static IActivityBuilder While(this IActivityBuilder activityBuilder, Action<While> setup = null, Action<IActivityBuilder> branch = null, string name = default) 
            => activityBuilder.Then(setup, branch, name);

        public static IActivityBuilder While(this IActivityBuilder activityBuilder, IWorkflowExpression<bool> condition, Action<IActivityBuilder> branch = null, string name = default) 
            => activityBuilder.While(x => x.Condition = condition, branch, name);

        public static IActivityBuilder While(this IActivityBuilder activityBuilder, Func<ActivityExecutionContext, bool> condition, Action<IActivityBuilder> branch = null, string name = default) 
            => activityBuilder.While(x => x.Condition = new CodeExpression<bool>(condition), branch, name);

        public static IActivityBuilder While(this IActivityBuilder activityBuilder, Func<bool> condition, Action<IActivityBuilder> branch = null, string name = default) 
            => activityBuilder.While(x => x.Condition = new CodeExpression<bool>(condition), branch, name);

        public static IActivityBuilder Join(this IActivityBuilder activityBuilder, Action<Join> setup = null, Action<IActivityBuilder> branch = null, string name = default) 
            => activityBuilder.Then(setup, branch, name);

        public static IActivityBuilder IfElse(this IActivityBuilder activityBuilder, Action<IfElse> setup = null, Action<IActivityBuilder> branch = null, string name = default) 
            => activityBuilder.Then(setup, branch, name);

        public static IActivityBuilder Switch(this IActivityBuilder activityBuilder, Action<Switch> setup = null, Action<IActivityBuilder> branch = null, string name = default) 
            => activityBuilder.Then(setup, branch, name);

        public static IActivityBuilder Switch(this IActivityBuilder activityBuilder, IWorkflowExpression<string> value, IEnumerable<string> cases, Action<IActivityBuilder> branch = null, string name = default)
        {
            return activityBuilder.Switch(x =>
            {
                x.Value = value;
                x.Cases = cases.ToList();
            }, branch, name);
        }
        
        public static IActivityBuilder Switch(this IActivityBuilder activityBuilder, Func<ActivityExecutionContext, string> value, IEnumerable<string> cases, Action<IActivityBuilder> branch = null, string name = default) 
            => activityBuilder.Switch(new CodeExpression<string>(value), cases, branch, name);
        
        public static IActivityBuilder Switch(this IActivityBuilder activityBuilder, Func<string> value, IEnumerable<string> cases, Action<IActivityBuilder> branch = null, string name = default) 
            => activityBuilder.Switch(new CodeExpression<string>(value), cases, branch, name);
    }
}