using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Workflows
{
    public static class RunWorkflowExtensions
    {
        public static ISetupActivity<RunWorkflow> WithWorkflow(this ISetupActivity<RunWorkflow> activity, Func<ActivityExecutionContext, ValueTask<string>> value) => activity.Set(x => x.WorkflowDefinitionId, value!);
        public static ISetupActivity<RunWorkflow> WithWorkflow(this ISetupActivity<RunWorkflow> activity, Func<ActivityExecutionContext, string> value) => activity.Set(x => x.WorkflowDefinitionId, value);
        public static ISetupActivity<RunWorkflow> WithWorkflow(this ISetupActivity<RunWorkflow> activity, Func<ValueTask<string>> value) => activity.Set(x => x.WorkflowDefinitionId, value!);
        public static ISetupActivity<RunWorkflow> WithWorkflow(this ISetupActivity<RunWorkflow> activity, Func<string> value) => activity.Set(x => x.WorkflowDefinitionId, value);
        public static ISetupActivity<RunWorkflow> WithWorkflow(this ISetupActivity<RunWorkflow> activity, string value) => activity.Set(x => x.WorkflowDefinitionId, value);

        public static ISetupActivity<RunWorkflow> WithWorkflow<T>(this ISetupActivity<RunWorkflow> activity) where T : IWorkflow =>
            activity.WithWorkflow(
                async context =>
                {
                    var workflowRegistry = context.GetService<IWorkflowRegistry>();
                    var workflow = (await workflowRegistry.GetWorkflowAsync<T>())!;
                    return workflow.Id;
                });

        public static ISetupActivity<RunWorkflow> WithNamedWorkflow(this ISetupActivity<RunWorkflow> activity, string name) =>
            activity.WithWorkflow(
                async context => 
                {
                    var workflowRegistry = context.GetService<IWorkflowRegistry>();
                    return (await workflowRegistry.FindByNameAsync(name, VersionOptions.Published).ConfigureAwait(continueOnCapturedContext: false))?.Id
                        ?? throw new ArgumentOutOfRangeException(nameof(name));
                });
        
        public static ISetupActivity<RunWorkflow> WithInput(this ISetupActivity<RunWorkflow> activity, Func<ActivityExecutionContext, ValueTask<object?>> value) => activity.Set(x => x.Input, value);
        public static ISetupActivity<RunWorkflow> WithInput(this ISetupActivity<RunWorkflow> activity, Func<ActivityExecutionContext, object?> value) => activity.Set(x => x.Input, value);
        public static ISetupActivity<RunWorkflow> WithInput(this ISetupActivity<RunWorkflow> activity, Func<ValueTask<object?>> value) => activity.Set(x => x.Input, value);
        public static ISetupActivity<RunWorkflow> WithInput(this ISetupActivity<RunWorkflow> activity, Func<object?> value) => activity.Set(x => x.Input, value);
        public static ISetupActivity<RunWorkflow> WithInput(this ISetupActivity<RunWorkflow> activity, object? value) => activity.Set(x => x.Input, value);
        
        public static ISetupActivity<RunWorkflow> WithCorrelationId(this ISetupActivity<RunWorkflow> activity, Func<ActivityExecutionContext, ValueTask<string?>> value) => activity.Set(x => x.CorrelationId, value);
        public static ISetupActivity<RunWorkflow> WithCorrelationId(this ISetupActivity<RunWorkflow> activity, Func<ActivityExecutionContext, string?> value) => activity.Set(x => x.CorrelationId, value);
        public static ISetupActivity<RunWorkflow> WithCorrelationId(this ISetupActivity<RunWorkflow> activity, Func<ValueTask<string?>> value) => activity.Set(x => x.CorrelationId, value);
        public static ISetupActivity<RunWorkflow> WithCorrelationId(this ISetupActivity<RunWorkflow> activity, Func<string?> value) => activity.Set(x => x.CorrelationId, value);
        public static ISetupActivity<RunWorkflow> WithCorrelationId(this ISetupActivity<RunWorkflow> activity, string? value) => activity.Set(x => x.CorrelationId, value);
        
        public static ISetupActivity<RunWorkflow> WithContextId(this ISetupActivity<RunWorkflow> activity, Func<ActivityExecutionContext, ValueTask<string?>> value) => activity.Set(x => x.ContextId, value);
        public static ISetupActivity<RunWorkflow> WithContextId(this ISetupActivity<RunWorkflow> activity, Func<ActivityExecutionContext, string?> value) => activity.Set(x => x.ContextId, value);
        public static ISetupActivity<RunWorkflow> WithContextId(this ISetupActivity<RunWorkflow> activity, Func<ValueTask<string?>> value) => activity.Set(x => x.ContextId, value);
        public static ISetupActivity<RunWorkflow> WithContextId(this ISetupActivity<RunWorkflow> activity, Func<string?> value) => activity.Set(x => x.ContextId, value);
        public static ISetupActivity<RunWorkflow> WithContextId(this ISetupActivity<RunWorkflow> activity, string? value) => activity.Set(x => x.ContextId, value);
        
        public static ISetupActivity<RunWorkflow> WithMode(this ISetupActivity<RunWorkflow> activity, Func<ActivityExecutionContext, ValueTask<RunWorkflow.RunWorkflowMode>> value) => activity.Set(x => x.Mode, value);
        public static ISetupActivity<RunWorkflow> WithMode(this ISetupActivity<RunWorkflow> activity, Func<ActivityExecutionContext, RunWorkflow.RunWorkflowMode> value) => activity.Set(x => x.Mode, value);
        public static ISetupActivity<RunWorkflow> WithMode(this ISetupActivity<RunWorkflow> activity, Func<ValueTask<RunWorkflow.RunWorkflowMode>> value) => activity.Set(x => x.Mode, value);
        public static ISetupActivity<RunWorkflow> WithMode(this ISetupActivity<RunWorkflow> activity, Func<RunWorkflow.RunWorkflowMode> value) => activity.Set(x => x.Mode, value);
        public static ISetupActivity<RunWorkflow> WithMode(this ISetupActivity<RunWorkflow> activity, RunWorkflow.RunWorkflowMode value) => activity.Set(x => x.Mode, value);
        
        public static ISetupActivity<RunWorkflow> WithTenantId(this ISetupActivity<RunWorkflow> activity, Func<ActivityExecutionContext, ValueTask<string?>> value) => activity.Set(x => x.TenantId, value);
        public static ISetupActivity<RunWorkflow> WithTenantId(this ISetupActivity<RunWorkflow> activity, Func<ActivityExecutionContext, string?> value) => activity.Set(x => x.TenantId, value);
        public static ISetupActivity<RunWorkflow> WithTenantId(this ISetupActivity<RunWorkflow> activity, Func<ValueTask<string?>> value) => activity.Set(x => x.TenantId, value);
        public static ISetupActivity<RunWorkflow> WithTenantId(this ISetupActivity<RunWorkflow> activity, Func<string?> value) => activity.Set(x => x.TenantId, value);
        public static ISetupActivity<RunWorkflow> WithTenantId(this ISetupActivity<RunWorkflow> activity, string? value) => activity.Set(x => x.TenantId, value);
        
        public static ISetupActivity<RunWorkflow> WithCustomAttributes(this ISetupActivity<RunWorkflow> activity, Func<ActivityExecutionContext, ValueTask<Variables?>> value) => activity.Set(x => x.CustomAttributes, value);
        public static ISetupActivity<RunWorkflow> WithCustomAttributes(this ISetupActivity<RunWorkflow> activity, Func<ActivityExecutionContext, Variables?> value) => activity.Set(x => x.CustomAttributes, value);
        public static ISetupActivity<RunWorkflow> WithCustomAttributes(this ISetupActivity<RunWorkflow> activity, Func<ValueTask<Variables?>> value) => activity.Set(x => x.CustomAttributes, value);
        public static ISetupActivity<RunWorkflow> WithCustomAttributes(this ISetupActivity<RunWorkflow> activity, Func<Variables?> value) => activity.Set(x => x.CustomAttributes, value);
        public static ISetupActivity<RunWorkflow> WithCustomAttributes(this ISetupActivity<RunWorkflow> activity, Variables? value) => activity.Set(x => x.CustomAttributes, value);
    }
}
