using System;
using System.Threading.Tasks;
using Elsa.Activities;
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
    }
}