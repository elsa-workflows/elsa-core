using System;
using System.Threading.Tasks;
using Elsa.Activities;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Services
{
    public static class OutcomeBuilderExtensions
    {
        public static IActivityBuilder Then(this IOutcomeBuilder builder, Func<WorkflowExecutionContext, Task<IActivityExecutionResult>> code) 
            => builder.Then<CodeActivity>(x => x.Function = code);

        public static IActivityBuilder Then(this IOutcomeBuilder builder, Func<WorkflowExecutionContext, Task> code)
        {
            return builder.Then(async context =>
            {
                await code(context);
                return new OutcomeResult(OutcomeNames.Done);
            });
        }
        
        public static IActivityBuilder Then(this IOutcomeBuilder builder, Func<Task> code)
        {
            return builder.Then(async context =>
            {
                await code();
                return new OutcomeResult(OutcomeNames.Done);
            });
        }
        
        public static IActivityBuilder Then(this IOutcomeBuilder builder, Func<WorkflowExecutionContext, IActivityExecutionResult> code)
        {
            return builder.Then(context =>
            {
                code(context);
                return Task.FromResult<IActivityExecutionResult>(new OutcomeResult(OutcomeNames.Done));
            });
        }
        
        public static IActivityBuilder Then(this IOutcomeBuilder builder, Action<WorkflowExecutionContext> code)
        {
            return builder.Then(context =>
            {
                code(context);
                return Task.FromResult<IActivityExecutionResult>(new OutcomeResult(OutcomeNames.Done));
            });
        }
        
        public static IActivityBuilder Then(this IOutcomeBuilder builder, Action code)
        {
            return builder.Then(context =>
            {
                code();
                return Task.FromResult<IActivityExecutionResult>(new OutcomeResult(OutcomeNames.Done));
            });
        }
    }
}