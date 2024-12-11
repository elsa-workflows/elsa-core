using System.Linq.Expressions;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Workflows;
using Elsa.Workflows.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static partial class ActivityExecutionContextExtensions
{
    /// <summary>
    /// Evaluates each input property of the activity.
    /// </summary>
    public static async Task EvaluateInputPropertiesAsync(this ActivityExecutionContext context)
    {
        var activityDescriptor = context.ActivityDescriptor;
        var inputDescriptors = activityDescriptor.Inputs.Where(x => x.AutoEvaluate).ToList();

        // Evaluate inputs.
        foreach (var inputDescriptor in inputDescriptors)
            await EvaluateInputPropertyAsync(context, activityDescriptor, inputDescriptor);

        context.SetHasEvaluatedProperties();
    }

    /// <summary>
    /// Evaluates the specified input property of the activity.
    /// </summary>
    public static async Task<T?> EvaluateInputPropertyAsync<TActivity, T>(this ActivityExecutionContext context, Expression<Func<TActivity, Input<T>>> propertyExpression)
    {
        var inputName = propertyExpression.GetProperty()!.Name;
        var input = await EvaluateInputPropertyAsync(context, inputName);
        return input.ConvertTo<T>();
    }

    /// <summary>
    /// Evaluates a specific input property of the activity.
    /// </summary>
    public static async Task<object?> EvaluateInputPropertyAsync(this ActivityExecutionContext context, string inputName)
    {
        var activity = context.Activity;
        var activityRegistryLookup = context.GetRequiredService<IActivityRegistryLookupService>();
        var activityDescriptor = await activityRegistryLookup.FindAsync(activity.Type) ?? throw new Exception("Activity descriptor not found");
        var inputDescriptor = activityDescriptor.GetWrappedInputPropertyDescriptor(activity, inputName);

        if (inputDescriptor == null)
            throw new Exception($"No input with name {inputName} could be found");

        return await EvaluateInputPropertyAsync(context, activityDescriptor, inputDescriptor);
    }

    /// <summary>
    /// Evaluates the specified input and sets the result in the activity execution context's memory space.
    /// </summary>
    /// <param name="context">The <see cref="ActivityExecutionContext"/> being extended.</param>
    /// <param name="input">The input to evaluate.</param>
    /// <typeparam name="T">The type of the input.</typeparam>
    /// <returns>The evaluated value.</returns>
    public static async Task<T?> EvaluateAsync<T>(this ActivityExecutionContext context, Input<T> input)
    {
        var evaluator = context.GetRequiredService<IExpressionEvaluator>();
        var memoryBlockReference = input.MemoryBlockReference();
        var value = await evaluator.EvaluateAsync(input, context.ExpressionExecutionContext);
        memoryBlockReference.Set(context, value);
        return value;
    }
    
    private static async Task<object?> EvaluateInputPropertyAsync(this ActivityExecutionContext context, ActivityDescriptor activityDescriptor, InputDescriptor inputDescriptor)
    {
        var activity = context.Activity;
        var defaultValue = inputDescriptor.DefaultValue;
        var value = defaultValue;
        var input = inputDescriptor.ValueGetter(activity);
        var identityGenerator = context.GetRequiredService<IIdentityGenerator>();

        if (inputDescriptor.IsWrapped)
        {
            var wrappedInput = (Input?)input;

            if (defaultValue != null && wrappedInput == null)
            {
                var typedInput = typeof(Input<>).MakeGenericType(inputDescriptor.Type);
                var valueExpression = new Literal(defaultValue)
                {
                    Id = identityGenerator.GenerateId(),
                };
                wrappedInput = (Input)Activator.CreateInstance(typedInput, valueExpression)!;
                inputDescriptor.ValueSetter(activity, wrappedInput);
            }
            else
            {
                var evaluator = context.GetRequiredService<IExpressionEvaluator>();
                var expressionExecutionContext = context.ExpressionExecutionContext;
                value = wrappedInput?.Expression != null ? await evaluator.EvaluateAsync(wrappedInput, expressionExecutionContext) : defaultValue;
            }

            var memoryReference = wrappedInput?.MemoryBlockReference();

            // When input is created from an activity provider, there may be no memory block reference.
            if (memoryReference?.Id != null!)
            {
                // Declare the input memory block on the current context. 
                context.ExpressionExecutionContext.Set(memoryReference, value!);
            }
        }
        else
        {
            value = input;
        }

        await StoreInputValueAsync(context, inputDescriptor, value);

        return value;
    }
    
    private static async Task StoreInputValueAsync(ActivityExecutionContext context, InputDescriptor inputDescriptor, object? value)
    {
        // Store the serialized input value in the activity state.
        // Serializing the value ensures we store a copy of the value and not a reference to the input, which may change over time.
        if (inputDescriptor.IsSerializable != false)
        {
            // TODO: Disable filtering for now until we redesign log sanitization.
            // var serializedValue = await context.GetRequiredService<ISafeSerializer>().SerializeToElementAsync(value);
            // var manager = context.GetRequiredService<IActivityStateFilterManager>();
            // var filterContext = new ActivityStateFilterContext(context, inputDescriptor, serializedValue, context.CancellationToken);
            // var filterResult = await manager.RunFiltersAsync(filterContext);
            context.ActivityState[inputDescriptor.Name] = value;
        }
    }
}