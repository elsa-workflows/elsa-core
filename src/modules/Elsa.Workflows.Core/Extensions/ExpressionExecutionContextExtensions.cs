using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ExpressionExecutionContextExtensions
{
    public static readonly object WorkflowExecutionContextKey = new();
    public static readonly object ActivityExecutionContextKey = new();
    public static readonly object InputKey = new();
    public static readonly object WorkflowKey = new();

    public static IDictionary<object, object> CreateActivityExecutionContextPropertiesFrom(WorkflowExecutionContext workflowExecutionContext, IDictionary<string, object> input) =>
        new Dictionary<object, object>
        {
            [WorkflowExecutionContextKey] = workflowExecutionContext,
            [InputKey] = input,
            [WorkflowKey] = workflowExecutionContext.Workflow,
        };
    
    public static IDictionary<object, object> CreateTriggerIndexingPropertiesFrom(Workflow workflow, IDictionary<string, object> input) =>
        new Dictionary<object, object>
        {
            [WorkflowKey] = workflow,
            [InputKey] = input
        };

    public static WorkflowExecutionContext GetWorkflowExecutionContext(this ExpressionExecutionContext context) => (WorkflowExecutionContext)context.TransientProperties[WorkflowExecutionContextKey];
    public static ActivityExecutionContext GetActivityExecutionContext(this ExpressionExecutionContext context) => (ActivityExecutionContext)context.TransientProperties[ActivityExecutionContextKey];
    public static IDictionary<string, object> GetInput(this ExpressionExecutionContext context) => (IDictionary<string, object>)context.TransientProperties[InputKey];
    public static T? GetInput<T>(this ExpressionExecutionContext context, string key) => context.GetInput(key).ConvertTo<T>();
    public static object? GetInput(this ExpressionExecutionContext context, string key) => context.GetInput().TryGetValue(key, out var value) ? value : default;

    public static T? Get<T>(this ExpressionExecutionContext context, Input<T>? input) => input != null ? context.GetBlock(input.MemoryBlockReference).Value.ConvertTo<T>() : default;
    public static T? Get<T>(this ExpressionExecutionContext context, Output output) => context.GetBlock(output.MemoryBlockReference).Value.ConvertTo<T>();
    public static object? Get(this ExpressionExecutionContext context, Output output) => context.GetBlock(output.MemoryBlockReference).Value;
    public static T? GetVariable<T>(this ExpressionExecutionContext context, string name) => (T?)context.GetVariable(name);
    public static T? GetVariable<T>(this ExpressionExecutionContext context) => context.GetVariable(typeof(T).Name).ConvertTo<T>();
    public static object? GetVariable(this ExpressionExecutionContext context, string name) => new Variable(name).Get(context);
    public static Variable SetVariable<T>(this ExpressionExecutionContext context, T? value) => context.SetVariable(typeof(T).Name, value);
    public static Variable SetVariable<T>(this ExpressionExecutionContext context, string name, T? value) => context.SetVariable(name, (object?)value);

    public static Variable SetVariable(this ExpressionExecutionContext context, string name, object? value)
    {
        var variable = new Variable(name, value);
        context.Set(variable, value);
        return variable;
    }

    public static void Set(this ExpressionExecutionContext context, Output? output, object? value)
    {
        if(output != null) context.Set(output.MemoryBlockReference(), value);
    }
    
    /// <summary>
    /// Returns a dictionary of memory block keys and values across scopes.
    /// </summary>
    public static IDictionary<string, object> ReadAndFlattenMemoryBlocks(this ExpressionExecutionContext context)
    {
        var currentRegister = context.Memory;
        var memoryBlocks = new Dictionary<string, object>();

        while (currentRegister != null)
        {
            foreach (var l in currentRegister.Blocks)
            {
                if (!memoryBlocks.ContainsKey(l.Key))
                    memoryBlocks.Add(l.Key, l.Value.Value!);
            }

            currentRegister = currentRegister.Parent;
        }

        return memoryBlocks;
    }
}