using System;
using System.Threading.Tasks;
using Elsa.Activities;
using Elsa.Builders;
using Elsa.Results;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Services
{
    public static class WorkflowBuilderExtensions
    {
        public static IActivityBuilder StartWith(this IFlowchartBuilder builder, Func<ActivityExecutionContext, Task<IActivityExecutionResult>> code) 
            => builder.StartWith<CodeActivity>(x => x.Function = code);
        
        public static IActivityBuilder StartWith(this IFlowchartBuilder builder, Func<ActivityExecutionContext, Task> code)
        {
            return builder.StartWith(async context =>
            {
                await code(context);
                return new OutcomeResult(OutcomeNames.Done);
            });
        }
        
        public static IActivityBuilder StartWith(this IFlowchartBuilder builder, Func<Task> code)
        {
            return builder.StartWith(async context =>
            {
                await code();
                return new OutcomeResult(OutcomeNames.Done);
            });
        }
        
        public static IActivityBuilder StartWith(this IFlowchartBuilder builder, Action code)
        {
            return builder.StartWith(context =>
            {
                code();
                return Task.FromResult<IActivityExecutionResult>(new OutcomeResult(OutcomeNames.Done));
            });
        }
    }
}