using Elsa.Expressions.Models;
using Elsa.Models;

namespace Elsa;

public static class ExpressionExecutionContextExtensions
{
    public static readonly object WorkflowKey = new();
    public static readonly object TransientPropertiesKey = new();
    public static readonly object InputKey = new();

    public static IDictionary<object, object> CreateApplicationPropertiesFrom(Workflow workflow, IDictionary<object, object> transientProperties, IDictionary<string, object> input) =>
        new Dictionary<object, object>
        {
            [WorkflowKey] = workflow,
            [TransientPropertiesKey] = transientProperties,
            [InputKey] = input
        };

    public static Workflow GetWorkflow(this ExpressionExecutionContext context) => (Workflow)context.ApplicationProperties[WorkflowKey];
    public static IDictionary<object, object> GetTransientProperties(this ExpressionExecutionContext context) => (IDictionary<object, object>)context.ApplicationProperties[TransientPropertiesKey];
    public static IDictionary<string, object> GetInput(this ExpressionExecutionContext context) => (IDictionary<string, object>)context.ApplicationProperties[InputKey];

    public static T? Get<T>(this ExpressionExecutionContext context, Input<T>? input) => input != null ? (T?)context.GetLocation(input.LocationReference).Value : default;
    public static T? Get<T>(this ExpressionExecutionContext context, Output output) => (T?)context.GetLocation(output.LocationReference).Value;
    public static object? Get(this ExpressionExecutionContext context, Output output) => context.GetLocation(output.LocationReference).Value;
    public static T? GetVariable<T>(this ExpressionExecutionContext context, string name) => (T?)context.GetVariable(name);
    public static T? GetVariable<T>(this ExpressionExecutionContext context) => (T?)context.GetVariable(typeof(T).Name);
    public static object? GetVariable(this ExpressionExecutionContext context, string name) => new Variable(name).Get(context);

    public static Variable SetVariable<T>(this ExpressionExecutionContext context, T? value) => context.SetVariable(typeof(T).Name, value);
    public static Variable SetVariable<T>(this ExpressionExecutionContext context, string name, T? value) => context.SetVariable(name, (object?)value);

    public static Variable SetVariable(this ExpressionExecutionContext context, string name, object? value)
    {
        var variable = new Variable(name, value);
        context.Set(variable, value);
        return variable;
    }

    public static void Set(this ExpressionExecutionContext context, Output output, object? value)
    {
        //var convertedValue = output.ValueConverter?.Invoke(value) ?? value;
        var convertedValue = value;
        var targets = new[] { output.LocationReference }.Concat(output.Targets);
        foreach (var target in targets) context.Set(target, convertedValue);
    }
}