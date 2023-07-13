using System.Text.Json;
using Elsa.Expressions.Helpers;
using Elsa.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Models;
using Humanizer;

namespace Elsa.Workflows.Core.Services;

/// <inheritdoc />
public class ActivityFactory : IActivityFactory
{
    /// <inheritdoc />
    public IActivity Create(Type type, ActivityConstructorContext context)
    {
        // Backwards compatibility for older JSON schemas.
        var canStartWorkflow = GetBoolean(context.Element, "canStartWorkflow");
        var runAsynchronously = GetBoolean(context.Element, "runAsynchronously");
        var activityElement = context.Element;
        var activityDescriptor = context.ActivityDescriptor;
        var activity = (IActivity)context.Element.Deserialize(type, context.SerializerOptions)!;

        ReadSyntheticInputs(activityDescriptor, activity, activityElement, context.SerializerOptions);
        ReadSyntheticOutputs(activityDescriptor, activity, activityElement);
        
        activity.SetCanStartWorkflow(canStartWorkflow);
        activity.SetRunAsynchronously(runAsynchronously);

        return activity;
    }
    
    private void ReadSyntheticInputs(ActivityDescriptor activityDescriptor, IActivity activity, JsonElement activityRoot, JsonSerializerOptions options)
    {
        foreach (var inputDescriptor in activityDescriptor.Inputs.Where(x => x.IsSynthetic))
        {
            var inputName = inputDescriptor.Name;
            var propertyName = inputName.Camelize();
            var nakedType = inputDescriptor.Type;
            var wrappedType = typeof(Input<>).MakeGenericType(nakedType);

            if (!activityRoot.TryGetProperty(propertyName, out var propertyElement) || propertyElement.ValueKind == JsonValueKind.Null || propertyElement.ValueKind == JsonValueKind.Undefined) 
                continue;
            
            var isWrapped = propertyElement.ValueKind == JsonValueKind.Object && propertyElement.GetProperty("typeName").ValueKind != JsonValueKind.Undefined;

            if (isWrapped)
            {
                var json = propertyElement.ToString();
                var inputValue = JsonSerializer.Deserialize(json, wrappedType, options);

                activity.SyntheticProperties[inputName] = inputValue!;
            }
            else
            {
                activity.SyntheticProperties[inputName] = propertyElement.ConvertTo(inputDescriptor.Type)!;
            }
        }
    }

    private void ReadSyntheticOutputs(ActivityDescriptor activityDescriptor, IActivity activity, JsonElement activityRoot)
    {
        foreach (var outputDescriptor in activityDescriptor.Outputs.Where(x => x.IsSynthetic))
        {
            var outputName = outputDescriptor.Name;
            var propertyName = outputName.Camelize();
            var nakedType = outputDescriptor.Type;
            var wrappedType = typeof(Output<>).MakeGenericType(nakedType);

            if (!activityRoot.TryGetProperty(propertyName, out var propertyElement) || propertyElement.ValueKind == JsonValueKind.Null || propertyElement.ValueKind == JsonValueKind.Undefined)
                continue;

            var memoryReferenceElement = propertyElement.GetProperty("memoryReference");

            if (!memoryReferenceElement.TryGetProperty("id", out var memoryReferenceIdElement))
                continue;

            var variable = new Variable
            {
                Id = memoryReferenceIdElement.GetString()!
            };
            variable.Name = variable.Id;

            var output = Activator.CreateInstance(wrappedType, variable)!;

            activity.SyntheticProperties[outputName] = output!;
        }
    }

    private static bool GetBoolean(JsonElement element, string propertyName)
    {
        var propertyNames = new[] { propertyName.Camelize(), propertyName.Pascalize() };

        foreach (var name in propertyNames)
        {
            if (element.TryGetProperty("customProperties", out var customPropertyElement))
            {
                if (customPropertyElement.TryGetProperty(name, out var canStartWorkflowElement))
                    return canStartWorkflowElement.GetBoolean();
            }

            if (element.TryGetProperty(propertyName.Camelize(), out var property) && property.GetBoolean())
                return true;
        }

        return false;
    }
}