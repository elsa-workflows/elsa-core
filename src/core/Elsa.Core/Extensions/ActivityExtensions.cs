using System.Reflection;
using Elsa.Contracts;
using Elsa.Models;

namespace Elsa.Extensions;

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
        
    public static IEnumerable<Output> GetOutputs(this IActivity activity)
    {
        var outputProps = activity.GetType().GetProperties().Where(x => typeof(Output).IsAssignableFrom(x.PropertyType)).ToList();

        var query =
            from outputProp in outputProps
            select (Output?)outputProp.GetValue(activity)
            into output
            where output != null
            select output;

        return query.Select(x => x!).ToList();
    }
        
        
        
    public static IEnumerable<Variable> GetVariables(this IActivity activity)
    {
        var properties = activity.GetType().GetProperties();
        var variableProps =  properties.Where(x => typeof(Variable).IsAssignableFrom(x.PropertyType)).ToList();
        var variablesProps =  properties.Where(x => typeof(IEnumerable<Variable>).IsAssignableFrom(x.PropertyType)).ToList();
        var variables = variableProps.Select(x => (Variable?)x.GetValue(activity)).Where(x => x != null).Select(x => x!).ToList();
        var manyVariables = variablesProps.Select(x => (IEnumerable<Variable>?)x.GetValue(activity)).Where(x => x != null).SelectMany(x => x!).ToList();

        return variables.Concat(manyVariables).ToList();
    }
        
    public static TDelegate GetDelegate<TDelegate>(this IActivity driver, string methodName) where TDelegate : Delegate
    {
        var driverType = driver!.GetType();
        var resumeMethodInfo = driverType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        return (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), driver, resumeMethodInfo);
    }

    public static ExecuteActivityDelegate GetResumeActivityDelegate(this IActivity driver, string resumeMethodName) => driver.GetDelegate<ExecuteActivityDelegate>(resumeMethodName);
    public static ActivityCompletionCallback GetActivityCompletionCallback(this IActivity driver, string completionMethodName) => driver.GetDelegate<ActivityCompletionCallback>(completionMethodName);
}