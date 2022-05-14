using System;
using Elsa.Builders;
using Elsa.Models;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Workflows
{
    public static class RunWorkflowBuilderExtensions
    {
        public static IActivityBuilder RunWorkflow(this IBuilder builder, Action<ISetupActivity<RunWorkflow>>? setup = default) => builder.Then(setup);
        public static IActivityBuilder RunWorkflow<T>(this IBuilder builder, RunWorkflow.RunWorkflowMode mode) where T : IWorkflow => builder.RunWorkflow(activity => activity.WithWorkflow<T>().WithMode(mode));
        public static IActivityBuilder RunWorkflow<T>(this IBuilder builder, RunWorkflow.RunWorkflowMode mode, object input) where T : IWorkflow => builder.RunWorkflow(activity => activity.WithWorkflow<T>().WithMode(mode).WithInput(input));
        public static IActivityBuilder RunWorkflow<T>(this IBuilder builder, RunWorkflow.RunWorkflowMode mode, Func<ActivityExecutionContext, object> input) where T : IWorkflow => builder.RunWorkflow(activity => activity.WithWorkflow<T>().WithMode(mode).WithInput(input));

        public static IActivityBuilder RunWorkflow<T>(this IBuilder builder, RunWorkflow.RunWorkflowMode mode, object input, string correlationId) where T : IWorkflow =>
            builder.RunWorkflow(activity => activity.WithWorkflow<T>().WithMode(mode).WithInput(input).WithCorrelationId(correlationId));

        public static IActivityBuilder RunWorkflow<T>(this IBuilder builder, RunWorkflow.RunWorkflowMode mode, object input, string correlationId, string contextId) where T : IWorkflow =>
            builder.RunWorkflow(activity => activity.WithWorkflow<T>().WithMode(mode).WithInput(input).WithCorrelationId(correlationId).WithContextId(contextId));

        public static IActivityBuilder RunWorkflow<T>(this IBuilder builder, RunWorkflow.RunWorkflowMode mode, string correlationId) where T : IWorkflow =>
            builder.RunWorkflow(activity => activity.WithWorkflow<T>().WithMode(mode).WithCorrelationId(correlationId));

        public static IActivityBuilder RunWorkflow(this IBuilder builder, string workflowDefinitionId, Func<ActivityExecutionContext, string> tenantId, RunWorkflow.RunWorkflowMode mode) =>
            builder.RunWorkflow(activity => activity.WithWorkflow(workflowDefinitionId).WithMode(mode).WithTenantId(tenantId));

        public static IActivityBuilder RunWorkflow(this IBuilder builder, string workflowDefinitionId, string tenantId, RunWorkflow.RunWorkflowMode mode) =>
            builder.RunWorkflow(activity => activity.WithWorkflow(workflowDefinitionId).WithMode(mode).WithTenantId(tenantId));

        public static IActivityBuilder RunWorkflow(this IBuilder builder, string workflowDefinitionId, string tenantId, RunWorkflow.RunWorkflowMode mode, object input) =>
            builder.RunWorkflow(activity => activity.WithWorkflow(workflowDefinitionId).WithMode(mode).WithInput(input).WithTenantId(tenantId));

        public static IActivityBuilder RunWorkflow(this IBuilder builder, string workflowDefinitionId, string tenantId, RunWorkflow.RunWorkflowMode mode, object input, string correlationId) =>
            builder.RunWorkflow(activity => activity.WithWorkflow(workflowDefinitionId).WithMode(mode).WithInput(input).WithCorrelationId(correlationId).WithTenantId(tenantId));

        public static IActivityBuilder RunWorkflow(this IBuilder builder, string workflowDefinitionId, string tenantId, RunWorkflow.RunWorkflowMode mode, object input, string correlationId, string contextId) =>
            builder.RunWorkflow(activity => activity.WithWorkflow(workflowDefinitionId).WithMode(mode).WithInput(input).WithCorrelationId(correlationId).WithContextId(contextId).WithTenantId(tenantId));

        public static IActivityBuilder RunWorkflow(this IBuilder builder, string workflowDefinitionId, string tenantId, RunWorkflow.RunWorkflowMode mode, string correlationId) =>
            builder.RunWorkflow(activity => activity.WithWorkflow(workflowDefinitionId).WithMode(mode).WithCorrelationId(correlationId).WithTenantId(tenantId));

        public static IActivityBuilder RunWorkflow(this IBuilder builder, string workflowDefinitionId, Variables customAttributes, RunWorkflow.RunWorkflowMode mode, string correlationId) =>
            builder.RunWorkflow(activity => activity.WithWorkflow(workflowDefinitionId).WithMode(mode).WithCorrelationId(correlationId).WithCustomAttributes(customAttributes));

        public static IActivityBuilder RunWorkflow(this IBuilder builder, Variables customAttributes, RunWorkflow.RunWorkflowMode mode, string correlationId) =>
            builder.RunWorkflow(activity => activity.WithMode(mode).WithCorrelationId(correlationId).WithCustomAttributes(customAttributes));

        public static IActivityBuilder RunWorkflow(this IBuilder builder, Variables customAttributes, RunWorkflow.RunWorkflowMode mode) =>
            builder.RunWorkflow(activity => activity.WithMode(mode).WithCustomAttributes(customAttributes));
    }
}