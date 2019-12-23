using System;
using System.Threading.Tasks;
using Elsa.Activities;
using Elsa.Results;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Services
{
    public static class WorkflowBuilderExtensions
    {
        public static IActivityBuilder StartWith(this IWorkflowBuilder activityBuilder, Func<ActivityExecutionContext, Task<IActivityExecutionResult>> code) 
            => activityBuilder.StartWith<CodeActivity>(x => x.Function = code);
        
        public static IActivityBuilder StartWith(this IWorkflowBuilder activityBuilder, Func<ActivityExecutionContext, Task> code)
        {
            return activityBuilder.StartWith(async context =>
            {
                await code(context);
                return new OutcomeResult(OutcomeNames.Done);
            });
        }
        
        public static IActivityBuilder StartWith(this IWorkflowBuilder activityBuilder, Func<Task> code)
        {
            return activityBuilder.StartWith(async context =>
            {
                await code();
                return new OutcomeResult(OutcomeNames.Done);
            });
        }
        
        public static IActivityBuilder StartWith(this IWorkflowBuilder activityBuilder, Action code)
        {
            return activityBuilder.StartWith(context =>
            {
                code();
                return Task.FromResult<IActivityExecutionResult>(new OutcomeResult(OutcomeNames.Done));
            });
        }
    }
}