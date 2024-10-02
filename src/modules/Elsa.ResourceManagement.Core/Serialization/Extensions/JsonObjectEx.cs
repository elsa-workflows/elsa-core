using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.ResourceManagement.Metadata.Builders;
using Elsa.ResourceManagement.Serialization.Settings;

namespace Elsa.ResourceManagement.Serialization.Extensions;

public static class JsonObjectEx
{
    public static void Merge<T>(this JsonObject obj, Action<T> mergeAction) where T : class, new()
    {
        var existingObj = obj[typeof(T).Name] as JsonObject;
        
        if (existingObj == null)
        {
            existingObj = JsonObjectEx.FromObject(new T(), ResourceBuilderSettings.IgnoreDefaultValuesSerializer);
            obj[typeof(T).Name] = existingObj;
        }

        var objToMerge = existingObj.ToObject<T>()!;
        mergeAction(objToMerge);
        obj.SetProperty(objToMerge);
    }
    
    public static void SetProperty<T>(this JsonObject obj, T value)
    {
        var jObject = JsonObjectEx.FromObject(value, ResourceBuilderSettings.IgnoreDefaultValuesSerializer);
        obj[typeof(T).Name] = jObject;
    }
    
    public static JsonObject Clone(this JsonObject value) => (JsonObject)value.DeepClone();
    public static JsonNode? Clone(this JsonNode? jsonNode) => jsonNode?.DeepClone();

    public static JsonObject? Merge(this JsonObject? jsonObject, JsonNode? content, JsonMergeSettings? settings = null)
    {
        if (jsonObject is null || content is not JsonObject jsonContent)
            return jsonObject;

        settings ??= new JsonMergeSettings();

        foreach (var item in jsonContent)
        {
            if (item.Value is null)
                continue;

            var existingProperty = jsonObject[item.Key];
            if (existingProperty is null)
            {
                jsonObject[item.Key] = item.Value.Clone();
                continue;
            }

            if (existingProperty is JsonObject jObject)
            {
                jObject.Merge(item.Value, settings);
                continue;
            }

            if (existingProperty is JsonArray jArray)
            {
                jArray.Merge(item.Value, settings);
                continue;
            }

            if (existingProperty is JsonValue || existingProperty.GetValueKind() != item.Value.GetValueKind())
            {
                if (item.Value.GetValueKind() != JsonValueKind.Null || settings?.MergeNullValueHandling == MergeNullValueHandling.Merge)
                    jsonObject[item.Key] = item.Value.Clone();
            }
        }

        return jsonObject;
    }

    /// <summary>
    /// Creates a <see cref="JsonObject"/> from an object.
    /// </summary>
    public static JsonObject? FromObject(object? obj, JsonSerializerOptions? options = null)
    {
        if (obj is JsonObject jsonObject)
            return jsonObject;

        if (obj is JsonElement jsonElement)
            return JsonObject.Create(jsonElement, JsonOptions.Node);

        return JsonObject.Create(JsonSerializer.SerializeToElement(obj, options ?? JsonOptions.Default), JsonOptions.Node);
    }
}