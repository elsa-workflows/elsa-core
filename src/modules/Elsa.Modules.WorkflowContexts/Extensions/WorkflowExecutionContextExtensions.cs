using Elsa.Models;
using Elsa.Modules.WorkflowContexts.Models;

namespace Elsa.Modules.WorkflowContexts.Extensions;

public static class WorkflowExecutionContextExtensions
{
    public static void SetWorkflowContext(this WorkflowExecutionContext workflowExecutionContext, WorkflowContext workflowContext, object? value) => workflowExecutionContext.TransientProperties.SetWorkflowContext(workflowContext, value);
    public static void SetWorkflowContext(this ExpressionExecutionContext expressionExecutionContext, WorkflowContext workflowContext, object? value) => expressionExecutionContext.TransientProperties.SetWorkflowContext(workflowContext, value);
    public static T? GetWorkflowContext<T, TProvider>(this WorkflowExecutionContext workflowExecutionContext) => (T?)workflowExecutionContext.GetWorkflowContext(new WorkflowContext(typeof(TProvider)));
    public static T? GetWorkflowContext<T, TProvider>(this ExpressionExecutionContext expressionExecutionContext) => (T?)expressionExecutionContext.TransientProperties.GetWorkflowContext(new WorkflowContext(typeof(TProvider)));
    public static object? GetWorkflowContext(this WorkflowExecutionContext workflowExecutionContext, WorkflowContext workflowContext) => workflowExecutionContext.TransientProperties.GetWorkflowContext(workflowContext);
    public static object? GetWorkflowContext(this ExpressionExecutionContext expressionExecutionContext, WorkflowContext workflowContext) => expressionExecutionContext.TransientProperties.GetWorkflowContext(workflowContext);

    private static void SetWorkflowContext(this IDictionary<object, object?> transientProperties, WorkflowContext workflowContext, object? value)
    {
        var contextDictionary = transientProperties.GetOrAdd("WorkflowContexts", () => new Dictionary<Type, object?>())!;
        contextDictionary[workflowContext.ProviderType] = value;
    }

    private static object? GetWorkflowContext(this IDictionary<object, object?> transientProperties, WorkflowContext workflowContext)
    {
        var contextDictionary = transientProperties.GetOrAdd("WorkflowContexts", () => new Dictionary<Type, object?>())!;
        return contextDictionary[workflowContext.ProviderType];
    }
}