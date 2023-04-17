using Elsa.Expressions.Models;
using Elsa.WorkflowContexts.Models;
using Elsa.Workflows.Core.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extension methods to <see cref="WorkflowExecutionContext"/>.
/// </summary>
public static class WorkflowExecutionContextExtensions
{
    private static readonly object WorkflowContextsKey = new();
    
    /// <summary>
    /// Sets the specified workflow context value.
    /// </summary>
    /// <param name="workflowExecutionContext">The workflow execution context.</param>
    /// <param name="providerType">The type of the workflow context provider.</param>
    /// <param name="value">The value to set.</param>
    public static void SetWorkflowContext(this WorkflowExecutionContext workflowExecutionContext, Type providerType, object value) => workflowExecutionContext.TransientProperties.SetWorkflowContext(providerType, value);

    /// <summary>
    /// Sets the specified workflow context value.
    /// </summary>
    /// <param name="expressionExecutionContext">The expression execution context.</param>
    /// <param name="providerType">The type of the workflow context provider.</param>
    /// <param name="value">The value to set.</param>
    public static void SetWorkflowContext(this ExpressionExecutionContext expressionExecutionContext, Type providerType, object value) => expressionExecutionContext.GetWorkflowExecutionContext().TransientProperties.SetWorkflowContext(providerType, value);

    /// <summary>
    /// Gets the workflow context value.
    /// </summary>
    /// <param name="workflowExecutionContext">The workflow execution context.</param>
    /// <typeparam name="T">The type of the workflow context value.</typeparam>
    /// <typeparam name="TProvider">The type of the workflow context provider.</typeparam>
    /// <returns>The workflow context value.</returns>
    public static T? GetWorkflowContext<T, TProvider>(this WorkflowExecutionContext workflowExecutionContext) => (T?)workflowExecutionContext.TransientProperties.GetWorkflowContextByProviderType(typeof(TProvider));

    /// <summary>
    /// Gets the workflow context value.
    /// </summary>
    /// <param name="expressionExecutionContext">The expression execution context.</param>
    /// <typeparam name="T">The type of the workflow context value.</typeparam>
    /// <typeparam name="TProvider">The type of the workflow context provider.</typeparam>
    /// <returns>The workflow context value.</returns>
    public static T GetWorkflowContext<TProvider, T>(this ExpressionExecutionContext expressionExecutionContext) => (T)expressionExecutionContext.GetWorkflowExecutionContext().TransientProperties.GetWorkflowContextByProviderType(typeof(TProvider))!;

    /// <summary>
    /// Gets the workflow context value.
    /// </summary>
    /// <param name="workflowExecutionContext">The workflow execution context.</param>
    /// <param name="providerType">The type of the workflow context provider.</param>
    /// <returns>The workflow context value.</returns>
    public static object GetWorkflowContext(this WorkflowExecutionContext workflowExecutionContext, Type providerType) => workflowExecutionContext.TransientProperties.GetWorkflowContext(providerType);

    /// <summary>
    /// Gets the workflow context value.
    /// </summary>
    /// <param name="expressionExecutionContext">The expression execution context.</param>
    /// <param name="providerType">The type of the workflow context provider.</param>
    /// <returns>The workflow context value.</returns>
    public static object? GetWorkflowContext(this ExpressionExecutionContext expressionExecutionContext, Type providerType) => expressionExecutionContext.GetWorkflowExecutionContext().TransientProperties.GetWorkflowContext(providerType);

    private static void SetWorkflowContext(this IDictionary<object, object> transientProperties, Type providerType, object value)
    {
        var contextDictionary = GetWorkflowContextDictionary(transientProperties);
        contextDictionary[providerType] = value;
    }

    private static object GetWorkflowContext(this IDictionary<object, object> transientProperties, Type providerType)
    {
        var contextDictionary = transientProperties.GetWorkflowContextDictionary();
        return contextDictionary[providerType];
    }

    private static object? GetWorkflowContextByProviderType(this IDictionary<object, object> transientProperties, Type providerType) =>
        transientProperties.FindWorkflowContext(x => x == providerType);

    private static object? FindWorkflowContext(this IDictionary<object, object> transientProperties, Func<Type, bool> filter)
    {
        var contextDictionary = transientProperties.GetWorkflowContextDictionary();

        var query =
            from entry in contextDictionary
            where filter(entry.Key)
            select entry.Value;

        return query.FirstOrDefault();
    }

    private static IDictionary<Type, object> GetWorkflowContextDictionary(this IDictionary<object, object> transientProperties) =>
        transientProperties.GetOrAdd(WorkflowContextsKey, () => new Dictionary<Type, object>());
}