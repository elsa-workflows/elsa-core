using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

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

    public static bool TryGetWorkflowExecutionContext(this ExpressionExecutionContext context, out WorkflowExecutionContext workflowExecutionContext) =>
        context.TransientProperties.TryGetValue(WorkflowExecutionContextKey, out workflowExecutionContext!);

    public static WorkflowExecutionContext GetWorkflowExecutionContext(this ExpressionExecutionContext context) => (WorkflowExecutionContext)context.TransientProperties[WorkflowExecutionContextKey];
    public static ActivityExecutionContext GetActivityExecutionContext(this ExpressionExecutionContext context) => (ActivityExecutionContext)context.TransientProperties[ActivityExecutionContextKey];
    public static bool TryGetActivityExecutionContext(this ExpressionExecutionContext context, out ActivityExecutionContext activityExecutionContext) => context.TransientProperties.TryGetValue(ActivityExecutionContextKey, out activityExecutionContext!);

    public static IDictionary<string, object> GetInput(this ExpressionExecutionContext context) => (IDictionary<string, object>)context.TransientProperties[InputKey];
    public static T? GetInput<T>(this ExpressionExecutionContext context, string key) => context.GetInput(key).ConvertTo<T>();
    public static object? GetInput(this ExpressionExecutionContext context, string key) => context.GetInput().TryGetValue(key, out var value) ? value : default;

    public static T? Get<T>(this ExpressionExecutionContext context, Input<T>? input) => input != null ? context.GetBlock(input.MemoryBlockReference).Value.ConvertTo<T>() : default;
    public static T? Get<T>(this ExpressionExecutionContext context, Output output) => context.GetBlock(output.MemoryBlockReference).Value.ConvertTo<T>();
    public static object? Get(this ExpressionExecutionContext context, Output output) => context.GetBlock(output.MemoryBlockReference).Value;
    public static T? GetVariableByName<T>(this ExpressionExecutionContext context, string name) => (T?)context.GetVariableByName(name)?.Value;

    private static Variable? GetVariableByName(this ExpressionExecutionContext context, string name)
    {
        foreach (var block in context.Memory.Blocks.Where(b => b.Value.Metadata is VariableBlockMetadata))
        {
            var metadata = block.Value.Metadata as VariableBlockMetadata;
            if (metadata!.Variable.Name == name)
                return metadata.Variable;
        }

        return context.ParentContext?.GetVariableByName(name);
    }

    public static Variable CreateVariable<T>(this ExpressionExecutionContext context, string name, T? value, Type? storageDriverType = null, Action<MemoryBlock>? configure = default)
    {
        var existingVariable = context.GetVariableByName(name);
        if(existingVariable != null)
            throw new Exception($"Variable {name} already exists in the context.");
        
        var variable = new Variable(name, value)
        {
            StorageDriverType = storageDriverType ?? typeof(WorkflowStorageDriver)
        };
        
        context.Set(variable, value, configure);
        return variable;
    }

    public static Variable SetVariable<T>(this ExpressionExecutionContext context, string name, T? value, Action<MemoryBlock>? configure = default)
    {
        var variable = context.GetVariableByName(name);
        if(variable is null)
            throw new Exception($"Variable {name} not found in the context.");

        variable.Value = value;
        variable.Set(context, value, configure);
        return variable;
    }

    public static void Set(this ExpressionExecutionContext context, Output? output, object? value, Action<MemoryBlock>? configure = default)
    {
        if (output != null) context.Set(output.MemoryBlockReference(), value, configure);
    }

    /// <summary>
    /// Returns a dictionary of memory block keys and values across scopes.
    /// </summary>
    public static IDictionary<string, object> ReadAndFlattenMemoryBlocks(this ExpressionExecutionContext context) =>
        context.FlattenMemoryBlocks().ToDictionary(x => x.Key, x => x.Value.Value!);

    /// <summary>
    /// Returns a dictionary of memory blocks across scopes.
    /// </summary>
    public static IDictionary<string, MemoryBlock> FlattenMemoryBlocks(this ExpressionExecutionContext context)
    {
        var currentContext = context;
        var memoryBlocks = new Dictionary<string, MemoryBlock>();

        while (currentContext != null)
        {
            var register = currentContext.Memory;
            foreach (var entry in register.Blocks)
            {
                if (!memoryBlocks.ContainsKey(entry.Key))
                    memoryBlocks.Add(entry.Key, entry.Value);
            }

            currentContext = currentContext.ParentContext;
        }

        return memoryBlocks;
    }

    /// <summary>
    /// Returns a the first context that contains a memory block with the specified ID.
    /// </summary>
    public static ExpressionExecutionContext? FindContextContainingBlock(this ExpressionExecutionContext context, string blockId)
    {
        return context.FindParent(x => x.Memory.HasBlock(blockId));
    }

    /// <summary>
    /// Returns the first context in the hierarchy that matches the specified predicate.
    /// </summary>
    /// <param name="context">The context to start searching from.</param>
    /// <param name="predicate">The predicate to match.</param>
    /// <returns>The first context that matches the predicate or <c>null</c> if no match was found.</returns>
    public static ExpressionExecutionContext? FindParent(this ExpressionExecutionContext context, Func<ExpressionExecutionContext, bool> predicate)
    {
        var currentContext = context;

        while (currentContext != null)
        {
            if (predicate(currentContext))
                return currentContext;

            currentContext = currentContext.ParentContext;
        }

        return null;
    }
}