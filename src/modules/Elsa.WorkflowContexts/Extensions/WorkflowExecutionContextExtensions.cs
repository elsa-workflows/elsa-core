using Elsa.Expressions.Models;
using Elsa.WorkflowContexts.Contracts;
using Elsa.Workflows.Core.Models;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extension methods to <see cref="WorkflowExecutionContext"/>.
/// </summary>
[PublicAPI]
public static class WorkflowExecutionContextExtensions
{
    private static readonly object WorkflowContextsKey = new();

    /// <summary>
    /// Sets a workflow context provider parameter.
    /// </summary>
    /// <param name="context">The workflow execution context.</param>
    /// <param name="providerType">The type of the workflow context provider.</param>
    /// <param name="value">The value to set.</param>
    public static void SetWorkflowContextParameter(this WorkflowExecutionContext context, Type providerType, object? value) => SetWorkflowContextParameter(context, providerType, null, value);

    /// <summary>
    /// Sets a workflow context provider parameter.
    /// </summary>
    /// <param name="context">The workflow execution context.</param>
    /// <param name="value">The value to set.</param>
    /// <typeparam name="T">The type of the workflow context provider.</typeparam>
    public static void SetWorkflowContextParameter<T>(this WorkflowExecutionContext context, object? value) where T : IWorkflowContextProvider => SetWorkflowContextParameter(context, typeof(T), null, value);

    /// <summary>
    /// Sets a workflow context provider parameter.
    /// </summary>
    /// <param name="context">The workflow execution context.</param>
    /// <param name="providerType">The type of the workflow context provider.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="value">The value to set.</param>
    public static void SetWorkflowContextParameter(this WorkflowExecutionContext context, Type providerType, string? parameterName, object? value)
    {
        var scopedParameterName = providerType.GetScopedParameterName(parameterName);
        context.SetProperty(scopedParameterName, value);
    }

    /// <summary>
    /// Sets a workflow context provider parameter.
    /// </summary>
    /// <param name="context">The workflow execution context.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="value">The value to set.</param>
    /// <typeparam name="T">The type of the workflow context provider.</typeparam>
    public static void SetWorkflowContextParameter<T>(this WorkflowExecutionContext context, string? parameterName, object? value) => SetWorkflowContextParameter(context, typeof(T), parameterName, value);

    /// <summary>
    /// Gets a workflow context provider parameter.
    /// </summary>
    /// <param name="context">The workflow execution context.</param>
    /// <param name="providerType">The type of the workflow context provider.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <typeparam name="T">The type of the parameter.</typeparam>
    /// <returns>The parameter value.</returns>
    public static T? GetWorkflowContextParameter<T>(this WorkflowExecutionContext context, Type providerType, string? parameterName = default)
    {
        var scopedParameterName = providerType.GetScopedParameterName(parameterName);
        return context.GetProperty<T>(scopedParameterName);
    }

    /// <summary>
    /// Gets a workflow context provider parameter.
    /// </summary>
    /// <param name="context">The workflow execution context.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <typeparam name="TProvider">The type of the workflow context provider.</typeparam>
    /// <typeparam name="TParameter">The type of the parameter.</typeparam>
    /// <returns>The parameter value.</returns>
    public static TParameter? GetWorkflowContextParameter<TProvider, TParameter>(this WorkflowExecutionContext context, string? parameterName = default) where TProvider : IWorkflowContextProvider
        => GetWorkflowContextParameter<TParameter>(context, typeof(TProvider), parameterName);

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