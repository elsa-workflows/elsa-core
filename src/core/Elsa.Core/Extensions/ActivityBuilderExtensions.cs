using System;
using System.Threading.Tasks;
using Elsa.Activities;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Services
{
    public static class ActivityBuilderExtensions
    {
        public static IActivityBuilder StartWith(this IActivityBuilder activityBuilder, Func<WorkflowExecutionContext, Task<IActivityExecutionResult>> code) 
            => activityBuilder.StartWith<CodeActivity>(x => x.Function = code);
        
        public static IActivityBuilder StartWith(this IActivityBuilder activityBuilder, Func<WorkflowExecutionContext, Task> code)
        {
            return activityBuilder.StartWith(async context =>
            {
                await code(context);
                return new OutcomeResult(OutcomeNames.Done);
            });
        }
        
        public static IActivityBuilder StartWith(this IActivityBuilder activityBuilder, Func<Task> code)
        {
            return activityBuilder.StartWith(async context =>
            {
                await code();
                return new OutcomeResult(OutcomeNames.Done);
            });
        }
        
        public static IActivityBuilder StartWith(this IActivityBuilder activityBuilder, Action code)
        {
            return activityBuilder.StartWith(context =>
            {
                code();
                return Task.FromResult<IActivityExecutionResult>(new OutcomeResult(OutcomeNames.Done));
            });
        }
        
        public static IActivityBuilder Then(this IActivityBuilder activityBuilder, Func<WorkflowExecutionContext, Task<IActivityExecutionResult>> code) 
            => activityBuilder.Then<CodeActivity>(x => x.Function = code);

        public static IActivityBuilder Then(this IActivityBuilder activityBuilder, Func<WorkflowExecutionContext, Task> code)
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
        
        public static IActivityBuilder Then(this IActivityBuilder activityBuilder, Func<WorkflowExecutionContext, IActivityExecutionResult> code)
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
    }
}