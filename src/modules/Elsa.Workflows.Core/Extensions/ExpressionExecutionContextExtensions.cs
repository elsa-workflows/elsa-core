using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extensions on <see cref="ExpressionExecutionContext"/>
/// </summary>
public static class ExpressionExecutionContextExtensions
{
    public static readonly object WorkflowExecutionContextKey = new();
    public static readonly object ActivityExecutionContextKey = new();
    public static readonly object InputKey = new();
    public static readonly object WorkflowKey = new();

    /// <summary>
    /// Creates a dictionary for the specified <see cref="WorkflowExecutionContext"/> and <see cref="ActivityExecutionContext"/>.
    /// </summary>
    public static IDictionary<object, object> CreateActivityExecutionContextPropertiesFrom(WorkflowExecutionContext workflowExecutionContext, IDictionary<string, object> input) =>
        new Dictionary<object, object>
        {
            [WorkflowExecutionContextKey] = workflowExecutionContext,
            [InputKey] = input,
            [WorkflowKey] = workflowExecutionContext.Workflow,
        };

    /// <summary>
    /// Creates a dictionary for the specified <see cref="WorkflowExecutionContext"/> and <see cref="ActivityExecutionContext"/>.
    /// </summary>
    public static IDictionary<object, object> CreateTriggerIndexingPropertiesFrom(Workflow workflow, IDictionary<string, object> input) =>
        new Dictionary<object, object>
        {
            [WorkflowKey] = workflow,
            [InputKey] = input
        };

    /// <summary>
    /// Returns the <see cref="Workflow"/> of the specified <see cref="ExpressionExecutionContext"/>
    /// </summary>
    public static bool TryGetWorkflowExecutionContext(this ExpressionExecutionContext context, out WorkflowExecutionContext workflowExecutionContext) =>
        context.TransientProperties.TryGetValue(WorkflowExecutionContextKey, out workflowExecutionContext!);

    /// <summary>
    /// Returns the <see cref="WorkflowExecutionContext"/> of the specified <see cref="ExpressionExecutionContext"/>
    /// </summary>
    public static WorkflowExecutionContext GetWorkflowExecutionContext(this ExpressionExecutionContext context) => (WorkflowExecutionContext)context.TransientProperties[WorkflowExecutionContextKey];
    
    /// <summary>
    /// Returns the <see cref="ActivityExecutionContext"/> of the specified <see cref="ExpressionExecutionContext"/>
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static ActivityExecutionContext GetActivityExecutionContext(this ExpressionExecutionContext context) => (ActivityExecutionContext)context.TransientProperties[ActivityExecutionContextKey];
    
    /// <summary>
    /// Returns the <see cref="ActivityExecutionContext"/> of the specified <see cref="ExpressionExecutionContext"/> 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="activityExecutionContext"></param>
    /// <returns></returns>
    public static bool TryGetActivityExecutionContext(this ExpressionExecutionContext context, out ActivityExecutionContext activityExecutionContext) => context.TransientProperties.TryGetValue(ActivityExecutionContextKey, out activityExecutionContext!);

    /// <summary>
    /// Returns the input of the current activity.
    /// </summary>
    public static IDictionary<string, object> GetInput(this ExpressionExecutionContext context) => (IDictionary<string, object>)context.TransientProperties[InputKey];
    
    /// <summary>
    /// Returns input sent to the workflow.
    /// </summary>
    public static T GetWorkflowInput<T>(this ExpressionExecutionContext context, string key) => context.GetActivityExecutionContext().GetWorkflowInput<T>(key);
    
    /// <summary>
    /// Returns input sent to the workflow.
    /// </summary>
    public static T GetWorkflowInput<T>(this ExpressionExecutionContext context) => context.GetActivityExecutionContext().GetWorkflowInput<T>();
    
    /// <summary>
    /// Returns the value of the specified input.
    /// </summary>
    public static T? GetInput<T>(this ExpressionExecutionContext context, string key) => context.GetInput(key).ConvertTo<T>();
    
    /// <summary>
    /// Returns the value of the specified input.
    /// </summary>
    public static object? GetInput(this ExpressionExecutionContext context, string key) => context.GetInput().TryGetValue(key, out var value) ? value : default;

    /// <summary>
    /// Returns the value of the specified input.
    /// </summary>
    public static T? Get<T>(this ExpressionExecutionContext context, Input<T>? input) => input != null ? context.GetBlock(input.MemoryBlockReference).Value.ConvertTo<T>() : default;
    
    /// <summary>
    /// Returns the value of the specified output.
    /// </summary>
    public static T? Get<T>(this ExpressionExecutionContext context, Output output) => context.GetBlock(output.MemoryBlockReference).Value.ConvertTo<T>();
    
    /// <summary>
    /// Returns the value of the specified output.
    /// </summary>
    public static object? Get(this ExpressionExecutionContext context, Output output) => context.GetBlock(output.MemoryBlockReference).Value;
    
    
    /// <summary>
    /// Returns the value of the variable with the specified name.
    /// </summary>
    public static T? GetVariable<T>(this ExpressionExecutionContext context, string name) => (T?)context.GetVariable(name)?.Value;

    /// <summary>
    /// Returns the variable with the specified name.
    /// </summary>
    public static Variable? GetVariable(this ExpressionExecutionContext context, string name, bool localScopeOnly = false)
    {
        foreach (var block in context.Memory.Blocks.Where(b => b.Value.Metadata is VariableBlockMetadata))
        {
            var metadata = block.Value.Metadata as VariableBlockMetadata;
            if (metadata!.Variable.Name == name)
                return metadata.Variable;
        }
        
        return localScopeOnly ? null : context.ParentContext?.GetVariable(name);
    }

    /// <summary>
    /// Creates a named variable in the context.
    /// </summary>
    public static Variable CreateVariable<T>(this ExpressionExecutionContext context, string name, T? value, Type? storageDriverType = null, Action<MemoryBlock>? configure = default)
    {
        var existingVariable = context.GetVariable(name, localScopeOnly: true);
        
        if(existingVariable != null)
            throw new Exception($"Variable {name} already exists in the context.");
        
        var variable = new Variable(name, value)
        {
            StorageDriverType = storageDriverType ?? typeof(WorkflowStorageDriver)
        };
        
        // Find the first parent context that has a variable container.
        // If not found, use the current context.
        var variableContainerContext = context.GetVariableContainerContext();
        
        variableContainerContext.Set(variable, value, configure);
        return variable;
    }
    
    /// <summary>
    /// Returns the first parent context that contains a variable container.
    /// </summary>
    public static ExpressionExecutionContext GetVariableContainerContext(this ExpressionExecutionContext context)
    {
        return context.FindParent(x =>
        {
            var activityExecutionContext = x.TryGetActivityExecutionContext(out var activityExecutionContextResult) ? activityExecutionContextResult : null;
            return activityExecutionContext?.Activity is IVariableContainer;
        }) ?? context;
    }

    /// <summary>
    /// Sets the value of a named variable in the context.
    /// </summary>
    public static Variable SetVariable<T>(this ExpressionExecutionContext context, string name, T? value, Action<MemoryBlock>? configure = default)
    {
        var variable = context.GetVariable(name);
        
        if(variable == null)
            return CreateVariable(context, name, value, configure: configure);

        // Get the context where the variable is defined.
        var contextWithVariable = context.FindContextContainingBlock(variable.Id) ?? context;
        
        // Set the value on the variable.
        variable.Value = value;
        variable.Set(contextWithVariable, value, configure);
        
        // Return the variable.
        return variable;
    }

    /// <summary>
    /// Sets the output to the specified value.
    /// </summary>
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
                memoryBlocks.TryAdd(entry.Key, entry.Value);

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