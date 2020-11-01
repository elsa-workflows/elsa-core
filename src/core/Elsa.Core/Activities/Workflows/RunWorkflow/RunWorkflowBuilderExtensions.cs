using System;
using Elsa.Activities.ControlFlow;
using Elsa.Builders;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Workflows
{
    public static class RunWorkflowBuilderExtensions
    {
        public static IActivityBuilder RunWorkflow(this IBuilder builder, Action<ISetupActivity<RunWorkflow>>? setup = default) => builder.Then(setup);
        public static IActivityBuilder RunWorkflow<T>(this IBuilder builder, RunWorkflow.RunWorkflowMode mode) => builder.RunWorkflow(activity => activity.WithWorkflow<T>().WithMode(mode));
        public static IActivityBuilder RunWorkflow<T>(this IBuilder builder, RunWorkflow.RunWorkflowMode mode, object input) => builder.RunWorkflow(activity => activity.WithWorkflow<T>().WithMode(mode).WithInput(input));

        public static IActivityBuilder RunWorkflow<T>(this IBuilder builder, RunWorkflow.RunWorkflowMode mode, object input, string correlationId) =>
            builder.RunWorkflow(activity => activity.WithWorkflow<T>().WithMode(mode).WithInput(input).WithCorrelationId(correlationId));

        public static IActivityBuilder RunWorkflow<T>(this IBuilder builder, RunWorkflow.RunWorkflowMode mode, object input, string correlationId, string contextId) =>
            builder.RunWorkflow(activity => activity.WithWorkflow<T>().WithMode(mode).WithInput(input).WithCorrelationId(correlationId).WithContextId(contextId));

        public static IActivityBuilder RunWorkflow<T>(this IBuilder builder, RunWorkflow.RunWorkflowMode mode, string correlationId) =>
            builder.RunWorkflow(activity => activity.WithWorkflow<T>().WithMode(mode).WithCorrelationId(correlationId));
    }
}