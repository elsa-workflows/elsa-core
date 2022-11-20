using Elsa.Expressions.Models;
using Elsa.WorkflowContexts.Models;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Models;

namespace Elsa.WorkflowContexts.Extensions;

public static class WorkflowExecutionContextExtensions
{
    public static void SetWorkflowContext(this WorkflowExecutionContext workflowExecutionContext, WorkflowContext workflowContext, object value) => workflowExecutionContext.TransientProperties.SetWorkflowContext(workflowContext, value);
    public static void SetWorkflowContext(this ExpressionExecutionContext expressionExecutionContext, WorkflowContext workflowContext, object value) => expressionExecutionContext.GetWorkflowExecutionContext().TransientProperties.SetWorkflowContext(workflowContext, value);
    public static T? GetWorkflowContext<T, TProvider>(this WorkflowExecutionContext workflowExecutionContext) => (T?)workflowExecutionContext.TransientProperties.GetWorkflowContextByProviderType(typeof(TProvider));
    public static T? GetWorkflowContext<T, TProvider>(this ExpressionExecutionContext expressionExecutionContext) => (T?)expressionExecutionContext.GetWorkflowExecutionContext().TransientProperties.GetWorkflowContextByProviderType(typeof(TProvider));
    public static object? GetWorkflowContext(this WorkflowExecutionContext workflowExecutionContext, WorkflowContext workflowContext) => workflowExecutionContext.TransientProperties.GetWorkflowContext(workflowContext);
    public static object? GetWorkflowContext(this ExpressionExecutionContext expressionExecutionContext, WorkflowContext workflowContext) => expressionExecutionContext.GetWorkflowExecutionContext().TransientProperties.GetWorkflowContext(workflowContext);

    private static void SetWorkflowContext(this IDictionary<object, object> transientProperties, WorkflowContext workflowContext, object value)
    {
        var contextDictionary = transientProperties!.GetOrAdd("WorkflowContexts", () => new Dictionary<WorkflowContext, object>())!;
        contextDictionary[workflowContext] = value;
    }

    private static object? GetWorkflowContext(this IDictionary<object, object> transientProperties, WorkflowContext workflowContext)
    {
        var contextDictionary = transientProperties.GetWorkflowContextDictionary();
        return contextDictionary[workflowContext];
    }

    private static object? GetWorkflowContextByProviderType(this IDictionary<object, object> transientProperties, Type providerType) =>
        transientProperties.FindWorkflowContext(x => x.ProviderType == providerType);

    private static object? FindWorkflowContext(this IDictionary<object, object> transientProperties, Func<WorkflowContext, bool> filter)
    {
        var contextDictionary = transientProperties.GetWorkflowContextDictionary();

        var query =
            from entry in contextDictionary
            where filter(entry.Key)
            select entry.Value;

        return query.FirstOrDefault();
    }

    private static IDictionary<WorkflowContext, object> GetWorkflowContextDictionary(this IDictionary<object, object> transientProperties) =>
        transientProperties!.GetOrAdd("WorkflowContexts", () => new Dictionary<WorkflowContext, object>())!;
}