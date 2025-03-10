using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Common;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Humanizer;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extensions on <see cref="ExpressionExecutionContext"/>
/// </summary>
public static class ExpressionExecutionContextExtensions
{
    /// <summary>
    /// The key used to store the <see cref="WorkflowExecutionContext"/> in the <see cref="ExpressionExecutionContext.TransientProperties"/> dictionary.
    /// </summary>
    public static readonly object WorkflowExecutionContextKey = new();

    /// <summary>
    /// The key used to store the <see cref="ActivityExecutionContext"/> in the <see cref="ExpressionExecutionContext.TransientProperties"/> dictionary.
    /// </summary>
    public static readonly object ActivityExecutionContextKey = new();

    /// <summary>
    /// The key used to store the input in the <see cref="ExpressionExecutionContext.TransientProperties"/> dictionary.
    /// </summary>
    public static readonly object InputKey = new();

    /// <summary>
    /// The key used to store the workflow in the <see cref="ExpressionExecutionContext.TransientProperties"/> dictionary.
    /// </summary>
    public static readonly object WorkflowKey = new();
    
    /// <summary>
    /// The key used to store the activity in the <see cref="ExpressionExecutionContext.TransientProperties"/> dictionary.
    /// </summary>
    public static readonly object ActivityKey = new();

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
    public static bool TryGetWorkflowExecutionContext(this ExpressionExecutionContext context, out WorkflowExecutionContext workflowExecutionContext) => context.TransientProperties.TryGetValue(WorkflowExecutionContextKey, out workflowExecutionContext!);

    /// <summary>
    /// Returns the <see cref="WorkflowExecutionContext"/> of the specified <see cref="ExpressionExecutionContext"/>
    /// </summary>
    public static WorkflowExecutionContext GetWorkflowExecutionContext(this ExpressionExecutionContext context) => (WorkflowExecutionContext)context.TransientProperties[WorkflowExecutionContextKey];

    /// <summary>
    /// Returns the <see cref="ActivityExecutionContext"/> of the specified <see cref="ExpressionExecutionContext"/>
    /// </summary>
    public static ActivityExecutionContext GetActivityExecutionContext(this ExpressionExecutionContext context) => (ActivityExecutionContext)context.TransientProperties[ActivityExecutionContextKey];

    /// <summary>
    /// Returns the <see cref="ActivityExecutionContext"/> of the specified <see cref="ExpressionExecutionContext"/> 
    /// </summary>
    public static bool TryGetActivityExecutionContext(this ExpressionExecutionContext context, out ActivityExecutionContext activityExecutionContext) => context.TransientProperties.TryGetValue(ActivityExecutionContextKey, out activityExecutionContext!);
    
    /// <summary>
    /// Returns the <see cref="Activity"/> of the specified <see cref="ExpressionExecutionContext"/>
    /// </summary>
    public static IActivity GetActivity(this ExpressionExecutionContext context) => (IActivity)context.TransientProperties[ActivityKey];

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
    public static T? GetVariable<T>(this ExpressionExecutionContext context, string name)
    {
        var block = context.GetVariableBlock(name);
        return (T?)block?.Value;
    }

    /// <summary>
    /// Returns the variable with the specified name.
    /// </summary>
    public static Variable? GetVariable(this ExpressionExecutionContext context, string name, bool localScopeOnly = false)
    {
        var block = context.GetVariableBlock(name, localScopeOnly);
        return block?.Metadata is VariableBlockMetadata metadata ? metadata.Variable : null;
    }

    private static MemoryBlock? GetVariableBlock(this ExpressionExecutionContext context, string name, bool localScopeOnly = false)
    {
        foreach (var block in context.Memory.Blocks.Where(b => b.Value.Metadata is VariableBlockMetadata))
        {
            var metadata = block.Value.Metadata as VariableBlockMetadata;
            if (metadata!.Variable.Name == name)
                return block.Value;
        }

        return localScopeOnly ? null : context.ParentContext?.GetVariableBlock(name);
    }

    /// <summary>
    /// Creates a named variable in the context.
    /// </summary>
    public static Variable CreateVariable<T>(this ExpressionExecutionContext context, string name, T? value, Type? storageDriverType = null,
        Action<MemoryBlock>? configure = null)
    {
        var existingVariable = context.GetVariable(name, localScopeOnly: true);

        if (existingVariable != null)
            throw new($"Variable {name} already exists in the context.");

        var variable = new Variable(name, value)
        {
            StorageDriverType = storageDriverType ?? typeof(WorkflowInstanceStorageDriver)
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
    public static Variable SetVariable<T>(this ExpressionExecutionContext context, string name, T? value, Action<MemoryBlock>? configure = null)
    {
        var variable = context.GetVariable(name);

        if (variable == null)
            return CreateVariable(context, name, value, configure: configure);

        // Get the context where the variable is defined.
        var contextWithVariable = context.FindContextContainingBlock(variable.Id) ?? context;

        // Set the value on the variable.
        variable.Set(contextWithVariable, value, configure);

        // Return the variable.
        return variable;
    }

    /// <summary>
    /// Sets the output to the specified value.
    /// </summary>
    public static void Set(this ExpressionExecutionContext context, Output? output, object? value, Action<MemoryBlock>? configure = null)
    {
        if (output != null)
        {
            // Set the value on the output.
            var outputMemoryBlockReference = output.MemoryBlockReference();
            context.Set(outputMemoryBlockReference, value, configure);

            // If the referenced output is a workflow output definition, set the value on the workflow execution context.
            var workflowExecutionContext = context.GetWorkflowExecutionContext();
            var workflow = workflowExecutionContext.Workflow;
            var workflowOutputDefinition = workflow.Outputs.FirstOrDefault(x => x.Name == outputMemoryBlockReference.Id);

            if (workflowOutputDefinition != null)
                workflowExecutionContext.Output[workflowOutputDefinition.Name] = value!;
        }
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

    /// <summary>
    /// Returns the value of the specified variable.
    /// </summary>
    public static object GetVariableInScope(this ExpressionExecutionContext context, string variableName)
    {
        var variable = context.GetVariable(variableName);
        var value = variable?.Get(context);

        return ConvertIEnumerableToArray(value);
    }

    /// <summary>
    /// Gets all variables names in scope.
    /// </summary>
    public static IEnumerable<string> GetVariableNamesInScope(this ExpressionExecutionContext context) =>
        EnumerateVariablesInScope(context)
            .Select(x => x.Name)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct();

    /// <summary>
    /// Gets all variables in scope.
    /// </summary>
    public static IEnumerable<Variable> GetVariablesInScope(this ExpressionExecutionContext context) =>
        EnumerateVariablesInScope(context)
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .DistinctBy(x => x.Name);

    /// <summary>
    /// Sets the value of a named variable in the context.
    /// </summary>
    public static void SetVariableInScope(this ExpressionExecutionContext context, string variableName, object? value)
    {
        var q = from v in EnumerateVariablesInScope(context)
            where v.Name == variableName
            where v.TryGet(context, out _)
            select v;

        var variable = q.FirstOrDefault();

        if (variable != null)
            variable.Set(context, value);

        if (variable == null)
            CreateVariable(context, variableName, value);
    }

    /// <summary>
    /// Enumerates all variables in scope.
    /// </summary>
    public static IEnumerable<Variable> EnumerateVariablesInScope(this ExpressionExecutionContext context)
    {
        var currentScope = context;

        while (currentScope != null)
        {
            if (!currentScope.TryGetActivityExecutionContext(out var activityExecutionContext))
            {
                var variables = currentScope.Memory.Blocks.Values
                    .Where(x => x.Metadata is VariableBlockMetadata)
                    .Select(x => x.Metadata as VariableBlockMetadata)
                    .Select(x => x!.Variable)
                    .ToList();

                foreach (var variable in variables)
                    yield return variable;
            }
            else
            {
                var variables = activityExecutionContext.Variables;

                foreach (var variable in variables)
                    yield return variable;
            }

            currentScope = currentScope.ParentContext;
        }

        if (context.TryGetWorkflowExecutionContext(out var workflowExecutionContext))
        {
            if (workflowExecutionContext.Workflow.ResultVariable != null)
                yield return workflowExecutionContext.Workflow.ResultVariable;
        }
    }

    /// <summary>
    /// Returns the input value associated with the specified <see cref="InputDefinition"/> in the given <see cref="ExpressionExecutionContext"/>.
    /// </summary>
    /// <typeparam name="T">The type of the input value.</typeparam>
    /// <param name="context">The <see cref="ExpressionExecutionContext"/> containing the input.</param>
    /// <param name="inputDefinition">The <see cref="InputDefinition"/> specifying the input to retrieve.</param>
    /// <returns>The input value associated with the specified <see cref="InputDefinition"/> in the <see cref="ExpressionExecutionContext"/>.</returns>
    public static T? GetInput<T>(this ExpressionExecutionContext context, InputDefinition inputDefinition)
    {
        return context.GetInput<T>(inputDefinition.Name);
    }

    private static JsonSerializerOptions? _serializerOptions;

    private static JsonSerializerOptions GetSerializerOptions(ExpressionExecutionContext context)
    {
        if (_serializerOptions != null)
            return _serializerOptions;

        var serializerOptions = context.GetRequiredService<IJsonSerializer>().GetOptions().Clone();
        serializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
        _serializerOptions = serializerOptions;
        return serializerOptions;
    }

    /// <summary>
    /// Returns the value of the specified input.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="name">The name of the input.</param>
    /// <typeparam name="T">The type of the input.</typeparam>
    /// <returns>The value of the specified input.</returns>
    public static T? GetInput<T>(this ExpressionExecutionContext context, string name)
    {
        var value = context.GetInput(name);
        var serializerOptions = GetSerializerOptions(context);
        var converterOptions = new ObjectConverterOptions(serializerOptions);
        return value.ConvertTo<T>(converterOptions);
    }

    /// <summary>
    /// Returns the value of the specified input.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="name">The name of the input.</param>
    /// <returns>The value of the specified input.</returns>
    public static object? GetInput(this ExpressionExecutionContext context, string name)
    {
        if (context.IsContainedWithinCompositeActivity())
        {
            // If there's a variable in the current scope with the specified name, return that.
            var variable = context.GetVariable(name);

            if (variable != null)
                return variable.Get(context);
        }

        // Otherwise, return the input.
        var workflowExecutionContext = context.GetWorkflowExecutionContext();
        var input = workflowExecutionContext.Input;
        return input.TryGetValue(name, out var value) ? value : null;
    }

    /// <summary>
    /// Returns the value of the specified output.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="activityIdOrName">The ID or name of the activity.</param>
    /// <param name="outputName">The name of the output.</param>
    /// <returns>The value of the specified output.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the activity is not found.</exception>
    public static object? GetOutput(this ExpressionExecutionContext context, string activityIdOrName, string? outputName)
    {
        var workflowExecutionContext = context.GetWorkflowExecutionContext();
        var activityExecutionContext = context.GetActivityExecutionContext();
        var activity = activityExecutionContext.FindActivityByIdOrName(activityIdOrName);

        if (activity == null)
            throw new InvalidOperationException("Activity not found.");

        var outputRegister = workflowExecutionContext.GetActivityOutputRegister();
        var outputRecordCandidates = outputRegister.FindMany(activity.Id, outputName);
        var containerIds = activityExecutionContext.GetAncestors().Select(x => x.Id).ToList();
        var filteredOutputRecordCandidates = outputRecordCandidates.Where(x => containerIds.Contains(x.ContainerId));
        var outputRecord = filteredOutputRecordCandidates.FirstOrDefault();
        return outputRecord?.Value;
    }

    /// <summary>
    /// Returns all activity outputs.
    /// </summary>
    public static async IAsyncEnumerable<ActivityOutputs> GetActivityOutputs(this ExpressionExecutionContext context)
    {
        if (!context.TryGetActivityExecutionContext(out var activityExecutionContext))
            yield break;

        var useActivityName = activityExecutionContext.WorkflowExecutionContext.Workflow.CreatedWithModernTooling();
        var activitiesWithOutputs = activityExecutionContext.GetActivitiesWithOutputs();

        if (useActivityName)
            activitiesWithOutputs = activitiesWithOutputs.Where(x => !string.IsNullOrWhiteSpace(x.Activity.Name));

        await foreach (var activityWithOutput in activitiesWithOutputs)
        {
            var activity = activityWithOutput.Activity;
            var activityDescriptor = activityWithOutput.ActivityDescriptor;
            var activityIdentifier = useActivityName ? activity.Name : activity.Id;
            var activityIdPascalName = activityIdentifier.Pascalize();

            foreach (var output in activityDescriptor.Outputs)
            {
                var outputPascalName = output.Name.Pascalize();
                yield return new(activity.Id, activityIdPascalName, [
                    outputPascalName
                ]);
            }
        }
    }

    /// <summary>
    /// Returns a value indicating whether the current activity is inside a composite activity.
    /// </summary>
    public static bool IsContainedWithinCompositeActivity(this ExpressionExecutionContext context)
    {
        if (!context.TryGetActivityExecutionContext(out var activityExecutionContext))
            return false;

        // If the first workflow definition in the ancestor hierarchy and that workflow definition has a parent, then we are inside a composite activity.
        var firstWorkflowContext = activityExecutionContext.GetAncestors().FirstOrDefault(x => x.Activity is Workflow);

        return firstWorkflowContext?.ParentActivityExecutionContext != null;
    }

    /// <summary>
    /// Returns the result of the activity that was executed before the current activity.
    /// </summary>
    public static object? GetLastResult(this ExpressionExecutionContext context)
    {
        var workflowExecutionContext = context.GetWorkflowExecutionContext();
        return workflowExecutionContext.GetLastActivityResult();
    }

    /// <summary>
    /// Returns all activity inputs.
    /// </summary>
    public static IEnumerable<WorkflowInput> GetWorkflowInputs(this ExpressionExecutionContext context)
    {
        // Check if we are evaluating an expression during workflow execution.
        if (context.TryGetWorkflowExecutionContext(out var workflowExecutionContext))
        {
            var input = workflowExecutionContext.Input;

            foreach (var inputEntry in input)
            {
                var inputPascalName = inputEntry.Key.Pascalize();
                var inputValue = inputEntry.Value;
                yield return new(inputPascalName, inputValue);
            }
        }
        else
        {
            // We end up here when we are evaluating an expression during trigger indexing.
            // The scenario being that a workflow definition might have variables declared, that we want to be able to access from JavaScript expressions.
            foreach (var block in context.Memory.Blocks.Values)
            {
                if (block.Metadata is not VariableBlockMetadata variableBlockMetadata)
                    continue;

                var variable = variableBlockMetadata.Variable;
                var variablePascalName = variable.Name.Pascalize();
                yield return new(variablePascalName, block.Value);
            }
        }
    }

    private static object ConvertIEnumerableToArray(object? obj)
    {
        if (obj == null)
            return null!;

        // If it's not an IEnumerable or it's a string or dictionary, return the original object.
        if (obj is not IEnumerable enumerable || obj is string || obj is IDictionary)
            return obj;

        // If this is an async enumerable, return as-is.
        if (obj.GetType().Name == "AsyncIListEnumerableAdapter`1")
            return obj;

        // Use LINQ to convert the IEnumerable to an array.
        var elementType = obj.GetType().GetGenericArguments().FirstOrDefault();

        if (elementType == null)
            return obj;

        var toArrayMethod = typeof(Enumerable).GetMethod("ToArray")!.MakeGenericMethod(elementType);
        return toArrayMethod.Invoke(null, [
            enumerable
        ])!;
    }
}