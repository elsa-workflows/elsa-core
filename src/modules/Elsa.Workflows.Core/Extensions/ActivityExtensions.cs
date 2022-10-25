using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core;

public static class ActivityExtensions
{
    public static IEnumerable<Input> GetInputs(this IActivity activity)
    {
        var inputProps = activity.GetType().GetProperties().Where(x => typeof(Input).IsAssignableFrom(x.PropertyType)).ToList();

        var query =
            from inputProp in inputProps
            select (Input?)inputProp.GetValue(activity)
            into input
            where input != null
            select input;

        return query.Select(x => x!).ToList();
    }

    public static Input? GetInput(this IActivity activity, string inputName)
    {
        var inputProp = activity.GetType().GetProperties().FirstOrDefault(x => typeof(Input).IsAssignableFrom(x.PropertyType) && x.Name == inputName);

        if (inputProp == null)
            return null;

        return (Input?)inputProp.GetValue(activity);
    }

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

    public static IEnumerable<Variable> GetVariables(this IActivity activity)
    {
        var properties = activity.GetType().GetProperties();
        var variableProps = properties.Where(x => typeof(Variable).IsAssignableFrom(x.PropertyType)).ToList();
        var variablesProps = properties.Where(x => typeof(IEnumerable<Variable>).IsAssignableFrom(x.PropertyType)).ToList();
        var variables = variableProps.Select(x => (Variable?)x.GetValue(activity)).Where(x => x != null).Select(x => x!).ToList();
        var manyVariables = variablesProps.Select(x => (IEnumerable<Variable>?)x.GetValue(activity)).Where(x => x != null).SelectMany(x => x!).ToList();

        return variables.Concat(manyVariables).ToList();
    }

    public static TDelegate GetDelegate<TDelegate>(this IActivity activity, string methodName) where TDelegate : Delegate
    {
        var activityType = activity.GetType();
        var resumeMethodInfo = activityType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

        if (resumeMethodInfo == null)
        {
            if (activityType.BaseType != null)
                resumeMethodInfo = activityType.BaseType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (resumeMethodInfo == null)
                throw new Exception($"Can't find method name {methodName} on type {activityType} or its base type {activityType.BaseType}");
        }

        return (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), activity, resumeMethodInfo);
    }

    public static ExecuteActivityDelegate GetResumeActivityDelegate(this IActivity driver, string resumeMethodName) => driver.GetDelegate<ExecuteActivityDelegate>(resumeMethodName);
    public static ActivityCompletionCallback GetActivityCompletionCallback(this IActivity driver, string completionMethodName) => driver.GetDelegate<ActivityCompletionCallback>(completionMethodName);
}