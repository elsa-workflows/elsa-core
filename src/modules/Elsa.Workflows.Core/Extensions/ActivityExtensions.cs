using System.Reflection;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IActivity"/>.
/// </summary>
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
    /// Gets the input properties of the specified activity.
    /// </summary>
    public static IEnumerable<PropertyInfo> GetInputProperties(this IActivity activity) => activity.GetType().GetProperties().Where(x => typeof(Input).IsAssignableFrom(x.PropertyType)).ToList();

    /// <summary>
    /// Gets the method for the specified method name on the specified activity.
    /// </summary>
    public static TDelegate GetDelegate<TDelegate>(this IActivity activity, string methodName) where TDelegate : Delegate
    {
        var activityType = activity.GetType();
        var bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
        var resumeMethodInfo = activityType.GetMethod(methodName, bindingFlags);

        if (resumeMethodInfo == null)
        {
            if (activityType.BaseType != null)
                resumeMethodInfo = activityType.BaseType.GetMethod(methodName, bindingFlags);

            if (resumeMethodInfo == null)
                throw new Exception($"Can't find method name {methodName} on type {activityType} or its base type {activityType.BaseType}");
        }

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