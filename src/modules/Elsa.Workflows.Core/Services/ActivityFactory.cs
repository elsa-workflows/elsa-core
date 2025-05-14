﻿using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Expressions.Helpers;
using Elsa.Extensions;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Humanizer;

namespace Elsa.Workflows;

/// <inheritdoc />
public class ActivityFactory : IActivityFactory
{
    public IActivity Create(Type type, ActivityConstructorContext context)
    {
        // 1) Grab the raw text
        var raw = context.Element.GetRawText();

        // 2) Parse into a JsonNode so we can mutate
        var node = JsonNode.Parse(raw)!;

        // 3) Strip out any "_type" metadata wrappers
        StripTypeMetadata(node);

        // 4) Get a cleaned JSON string
        var cleanedJson = node.ToJsonString(new JsonSerializerOptions(JsonSerializerDefaults.Web));

        // 5) Parse it back to a JsonDocument so we can get a clean JsonElement
        using var doc = JsonDocument.Parse(cleanedJson);
        var cleanedElement = doc.RootElement.Clone();

        // 6) Deserialize into your IActivity
        var activity = (IActivity)JsonSerializer.Deserialize(cleanedJson, type, context.SerializerOptions)!;

        // 7) Pull out your boolean flags from the cleaned element
        var canStartWorkflow = GetBoolean(cleanedElement, "canStartWorkflow");
        var runAsynchronously = GetBoolean(cleanedElement, "runAsynchronously");

        // 8) If composite, setup
        if (activity is IComposite composite)
            composite.Setup();

        // 9) Your existing synthetic inputs/outputs routines, using the cleanedElement
        ReadSyntheticInputs(context.ActivityDescriptor, activity, cleanedElement, context.SerializerOptions);
        ReadSyntheticOutputs(context.ActivityDescriptor, activity, cleanedElement);

        // 10) Finally re‑apply those flags
        activity.SetCanStartWorkflow(canStartWorkflow);
        activity.SetRunAsynchronously(runAsynchronously);

        return activity;
    }

    /// <summary>
    /// Recursively remove any "_type" properties and unwrap any
    /// { "_type": "...", "items": [ ... ] } or "values": [ ... ] wrappers
    /// </summary>
    private void StripTypeMetadata(JsonNode node)
    {
        switch (node)
        {
            case JsonObject obj:
                // First recurse into each child
                foreach (var key in obj.Select(kvp => kvp.Key).ToList())
                {
                    var child = obj[key];
                    if (child is JsonObject wrapper && wrapper.ContainsKey("_type"))
                    {
                        // If it has an "items" array, replace the whole property with that array
                        if (wrapper["items"] is JsonArray items)
                        {
                            obj[key] = items;
                            StripTypeMetadata(items);
                            continue;
                        }

                        // Or a "values" array
                        if (wrapper["values"] is JsonArray values)
                        {
                            obj[key] = values;
                            StripTypeMetadata(values);
                            continue;
                        }
                    }

                    // Otherwise just recurse normally
                    if (child != null)
                        StripTypeMetadata(child);
                }

                // And remove any stray _type on this object
                obj.Remove("_type");
                break;

            case JsonArray arr:
                for (var i = 0; i < arr.Count; i++)
                {
                    var element = arr[i];
                    if (element is JsonObject w && w.ContainsKey("_type"))
                    {
                        if (w["items"] is JsonArray items)
                        {
                            arr[i] = items;
                            StripTypeMetadata(items);
                            continue;
                        }

                        if (w["values"] is JsonArray values)
                        {
                            arr[i] = values;
                            StripTypeMetadata(values);
                            continue;
                        }
                    }

                    if (element != null)
                        StripTypeMetadata(element);
                }

                break;

            // primitives—nothing to do
            default:
                break;
        }
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

            var variable = new Variable { Id = memoryReferenceIdElement.GetString()! };
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
