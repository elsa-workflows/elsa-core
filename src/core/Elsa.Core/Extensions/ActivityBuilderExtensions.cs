using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Activities;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.ControlFlow.Activities;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Results;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Services
{
    public static class ActivityBuilderExtensions
    {
        public static IActivityBuilder Then(this IActivityBuilder activityBuilder, Func<ActivityExecutionContext, Task<IActivityExecutionResult>> code)
            => activityBuilder.Then<CodeActivity>(x => x.Function = code);

        public static IActivityBuilder Then(this IActivityBuilder activityBuilder, Func<ActivityExecutionContext, Task> code)
        {
            return activityBuilder.Then(async context =>
            {
                await code(context);
                return new OutcomeResult(OutcomeNames.Done);
            });
        }

        public static IActivityBuilder Then(this IActivityBuilder activityBuilder, Func<Task> code)
        {
            return activityBuilder.Then(async context =>
            {
                await code();
                return new OutcomeResult(OutcomeNames.Done);
            });
        }

        public static IActivityBuilder Then(this IActivityBuilder activityBuilder, Func<ActivityExecutionContext, IActivityExecutionResult> code)
        {
            return activityBuilder.Then(context =>
            {
                code(context);
                return Task.FromResult<IActivityExecutionResult>(new OutcomeResult(OutcomeNames.Done));
            });
        }

        public static IActivityBuilder Then(this IActivityBuilder activityBuilder, Action<ActivityExecutionContext> code)
        {
            return activityBuilder.Then(context =>
            {
                code(context);
                return Task.FromResult<IActivityExecutionResult>(new OutcomeResult(OutcomeNames.Done));
            });
        }

        public static IActivityBuilder Then(this IActivityBuilder activityBuilder, Action code)
        {
            return activityBuilder.Then(context =>
            {
                code();
                return Task.FromResult<IActivityExecutionResult>(new OutcomeResult(OutcomeNames.Done));
            });
        }

        public static IActivityBuilder Add(this IActivityBuilder activityBuilder, Func<ActivityExecutionContext, Task<IActivityExecutionResult>> code)
            => activityBuilder.Add<CodeActivity>(x => x.Function = code);

        public static IActivityBuilder Add(this IActivityBuilder activityBuilder, Func<ActivityExecutionContext, Task> code)
        {
            return activityBuilder.Add(async context =>
            {
                await code(context);
                return new OutcomeResult(OutcomeNames.Done);
            });
        }

        public static IActivityBuilder Add(this IActivityBuilder activityBuilder, Func<Task> code)
        {
            return activityBuilder.Add(async context =>
            {
                await code();
                return new OutcomeResult(OutcomeNames.Done);
            });
        }

        public static IActivityBuilder Add(this IActivityBuilder activityBuilder, Func<ActivityExecutionContext, IActivityExecutionResult> code)
        {
            return activityBuilder.Add(context =>
            {
                code(context);
                return Task.FromResult<IActivityExecutionResult>(new OutcomeResult(OutcomeNames.Done));
            });
        }

        public static IActivityBuilder Add(this IActivityBuilder activityBuilder, Action<ActivityExecutionContext> code)
        {
            return activityBuilder.Add(context =>
            {
                code(context);
                return Task.FromResult<IActivityExecutionResult>(new OutcomeResult(OutcomeNames.Done));
            });
        }

        public static IActivityBuilder Add(this IActivityBuilder activityBuilder, Action code)
        {
            return activityBuilder.Add(context =>
            {
                code();
                return Task.FromResult<IActivityExecutionResult>(new OutcomeResult(OutcomeNames.Done));
            });
        }

        public static IActivityBuilder SetVariable(this IActivityBuilder activityBuilder, Action<SetVariable> setup = null, string name = default)
        {
            return activityBuilder.Then(setup, name: name);
        }

        public static IActivityBuilder SetVariable<T>(this IActivityBuilder activityBuilder, string variableName, T expression, string name = default) where T:IWorkflowExpression
        {
            return activityBuilder.SetVariable(x =>
            {
                x.VariableName = variableName;
                x.Value = expression;
            }, name);
        }
        
        public static IActivityBuilder SetVariable<T>(this IActivityBuilder activityBuilder, string variableName, Func<ActivityExecutionContext, T> value, string name = default)
        {
            return activityBuilder.SetVariable(variableName, new CodeExpression<T>(value), name);
        }

        public static IActivityBuilder SetVariable<T>(this IActivityBuilder activityBuilder, string variableName, Func<T> value, string name = default)
        {
            return activityBuilder.SetVariable(variableName, new CodeExpression<T>(value), name);
        }
        
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

        public static IActivityBuilder For(
            this IActivityBuilder activityBuilder, 
            Action<For> setup = null, 
            Action<IActivityBuilder> branch = null, 
            string name = default)
            => activityBuilder.Then(setup, branch, name);

        public static IActivityBuilder For(
            this IActivityBuilder activityBuilder, 
            IWorkflowExpression<int> start, 
            IWorkflowExpression<int> end, 
            IWorkflowExpression<int> step,
            IActivity activity,
            Action<IActivityBuilder> branch = null, 
            string name = default) =>
            activityBuilder.For(x =>
            {
                x.Start = start;
                x.End = end;
                x.Step = step;
                x.Activity = activity;
            }, branch, name);

        public static IActivityBuilder For(
            this IActivityBuilder activityBuilder, 
            Func<ActivityExecutionContext, int> start, 
            Func<ActivityExecutionContext, int> end, 
            Func<ActivityExecutionContext, int> step,
            IActivity activity,
            Action<IActivityBuilder> branch = null, 
            string name = default) =>
            activityBuilder.For(new CodeExpression<int>(start), new CodeExpression<int>(end), new CodeExpression<int>(step), activity);

        public static IActivityBuilder For(
            this IActivityBuilder activityBuilder, 
            Func<ActivityExecutionContext, int> start, 
            Func<ActivityExecutionContext, int> end,
            IActivity activity,
            Action<IActivityBuilder> branch = null, 
            string name = default) =>
            activityBuilder.For(new CodeExpression<int>(start), new CodeExpression<int>(end), new CodeExpression<int>(() => 1), activity);

        public static IActivityBuilder For(
            this IActivityBuilder activityBuilder, 
            Func<int> start, 
            Func<int> end,
            IActivity activity,
            Action<IActivityBuilder> branch = null, 
            string name = default) =>
            activityBuilder.For(new CodeExpression<int>(start), new CodeExpression<int>(end), new CodeExpression<int>(() => 1), activity);

        public static IActivityBuilder For(
            this IActivityBuilder activityBuilder,
            int start,
            int end,
            IActivity activity,
            Action<IActivityBuilder> branch = null,
            string name = default) =>
            activityBuilder.For(() => start, () => end, activity);
    }
}