using System.Linq.Expressions;
using System.Reflection;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IActivity"/>.
/// </summary>
[PublicAPI]
public static class ActivityExtensions
{
    /// <summary>
    /// Gets the input properties of the specified activity.
    /// </summary>
    public static IDictionary<string, Input> GetNamedInputs(this IActivity activity)
    {
        var inputProps = activity.GetInputProperties().ToList();

        var query =
            from inputProp in inputProps
            let inputValue = (Input?)inputProp.GetValue(activity)
            where inputValue != null
            select (inputProp, inputValue);

        return query.DistinctBy(x => x.inputProp.Name).ToDictionary(x => x.inputProp.Name, x => x.inputValue);
    }

    /// <summary>
    /// Gets the input properties of the specified activity.
    /// </summary>
    public static IEnumerable<Input> GetInputs(this IActivity activity) => GetNamedInputs(activity).Values;

    /// <summary>
    /// Gets the input with the specified name.
    /// </summary>
    public static Input? GetInput(this IActivity activity, string inputName) => GetNamedInputs(activity).TryGetValue(inputName, out var input) ? input : null;

    /// <summary>
    /// Gets the output properties of the specified activity.
    /// </summary>
    public static IEnumerable<NamedOutput> GetOutputs(this IActivity activity)
    {
        var outputProps = activity.GetType().GetProperties().Where(x => typeof(Output).IsAssignableFrom(x.PropertyType)).ToList();

        var query =
            from outputProp in outputProps
            let output = (Output?)outputProp.GetValue(activity)
            where output != null
            select new NamedOutput(outputProp.Name, output);

        return query.Select(x => x!).ToList();
    }

    /// <summary>
    /// Gets the output with the specified name.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <param name="context">The context.</param>
    /// <param name="outputName">Name of the output.</param>
    /// <returns>The output value.</returns>
    public static object? GetOutput(this IActivity activity, ExpressionExecutionContext context, string? outputName = default)
    {
        var workflowExecutionContext = context.GetWorkflowExecutionContext();
        var activityNode = workflowExecutionContext.FindNodeByActivity(activity);
        var outputRegister = workflowExecutionContext.GetActivityOutputRegister();
        var output = outputRegister.FindOutputByNodeId(activityNode.NodeId, outputName);
        return output;
    }

    /// <summary>
    /// Gets the output with the specified name.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <param name="context">The context.</param>
    /// <param name="outputName">Name of the output.</param>
    /// <typeparam name="T">The type of the output.</typeparam>
    /// <returns>The output value.</returns>
    public static T? GetOutput<T>(this IActivity activity, ExpressionExecutionContext context, string outputName)
    {
        var outputValue = activity.GetOutput(context, outputName);
        return outputValue == null ? default! : outputValue.ConvertTo<T>();
    }

    /// <summary>
    /// Gets the output with the specified name.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <param name="context">The context.</param>
    /// <param name="outputExpression">The output expression.</param>
    /// <typeparam name="TActivity">The type of the activity.</typeparam>
    /// <typeparam name="T">The type of the output.</typeparam>
    /// <returns>The output value.</returns>
    public static T? GetOutput<TActivity, T>(this TActivity activity, ExpressionExecutionContext context, Expression<Func<TActivity, object?>> outputExpression)
    {
        var outputName = outputExpression.GetPropertyName();
        return ((IActivity)activity!).GetOutput<T>(context, outputName);
    }

    /// <summary>
    /// Gets the Result output of the specified activity.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <param name="context">The context.</param>
    /// <typeparam name="T">The type as which to return the output.</typeparam>
    /// <returns>The output value.</returns>
    public static T? GetResult<T>(this IActivity activity, ExpressionExecutionContext context)
    {
        var value = GetResult(activity, context);
        return value == null ? default! : value.ConvertTo<T>();
    }

    /// <summary>
    /// Gets the Result output of the specified activity.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <param name="context">The context.</param>
    /// <returns>The output value.</returns>
    public static object? GetResult(this IActivity activity, ExpressionExecutionContext context)
    {
        return activity.GetOutput(context, ActivityOutputRegister.DefaultOutputName);
    }

    /// <summary>
    /// Gets the Result output of the specified activity.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="activity">The activity.</param>
    /// <returns>The output value.</returns>
    public static object? GetResult(this ExpressionExecutionContext context, IActivity activity)
    {
        return activity.GetOutput(context, ActivityOutputRegister.DefaultOutputName);
    }

    /// <summary>
    /// Gets the result of the last activity.
    /// </summary>
    /// <param name="context">The context.</param>
    public static T? GetLastResult<T>(this ExpressionExecutionContext context)
    {
        var value = GetLastResult(context);
        return value == null ? default! : value.ConvertTo<T>();
    }

    /// <summary>
    /// Gets the result of the last activity.
    /// </summary>
    /// <param name="context">The context.</param>
    public static object? GetLastResult(this ExpressionExecutionContext context)
    {
        var workflowExecutionContext = context.GetWorkflowExecutionContext();
        return workflowExecutionContext.GetLastActivityResult();
    }

    /// <summary>
    /// Gets the input properties of the specified activity.
    /// </summary>
    public static IEnumerable<PropertyInfo> GetInputProperties(this IActivity activity) => activity.GetType().GetProperties().Where(x => typeof(Input).IsAssignableFrom(x.PropertyType)).ToList();

    /// <summary>
    /// Gets the method for the specified method name on the specified activity.
    /// </summary>
    public static TDelegate GetDelegate<TDelegate>(this IActivity activity, string methodName) where TDelegate : Delegate
    {
        var activityType = activity.GetType();
        const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
        var resumeMethodInfo = default(MethodInfo?);
        var currentType = activityType;

        while (currentType != null && resumeMethodInfo == null)
        {
            resumeMethodInfo = currentType.GetMethod(methodName, bindingFlags);
            currentType = currentType.BaseType;
        }

        if (resumeMethodInfo == null)
            throw new Exception($"Can't find method name {methodName} on type {activityType} or its base type {activityType.BaseType}");

        return resumeMethodInfo.IsStatic ? (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), resumeMethodInfo) : (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), activity, resumeMethodInfo);
    }

    /// <summary>
    /// Gets the Resume method for the specified activity.
    /// </summary>
    public static ExecuteActivityDelegate GetResumeActivityDelegate(this IActivity driver, string resumeMethodName) => driver.GetDelegate<ExecuteActivityDelegate>(resumeMethodName);

    /// <summary>
    /// Gets the Child Activity Completed method for the specified activity.
    /// </summary>
    public static ActivityCompletionCallback GetActivityCompletionCallback(this IActivity driver, string completionMethodName) => driver.GetDelegate<ActivityCompletionCallback>(completionMethodName);
}