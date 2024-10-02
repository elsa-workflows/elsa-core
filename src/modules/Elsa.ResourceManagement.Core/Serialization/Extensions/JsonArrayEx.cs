using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.ResourceManagement.Serialization.Settings;

namespace Elsa.ResourceManagement.Serialization.Extensions;

public static class JsonArrayEx
{
    /// <summary>
    /// Whether this <see cref="JsonArray"/> contains the provided <see cref="JsonValue"/> or not.
    /// </summary>
    public static bool ContainsValue(this JsonArray? jsonArray, JsonValue? value)
    {
        if (jsonArray is null || value is null)
            return false;

        foreach (var item in jsonArray)
        {
            if (item is not JsonValue)
                continue;

            if (JsonNode.DeepEquals(item, value))
                return true;
        }

        return false;
    }
    
    /// <summary>
    /// Merge the specified resource into this <see cref="JsonArray"/> using <see cref="JsonMergeSettings"/>.
    /// </summary>
    internal static JsonArray? Merge(this JsonArray? jsonArray, JsonNode? resource, JsonMergeSettings? settings = null)
    {
        if (jsonArray is null || resource is not JsonArray jsonResource)
        {
            return jsonArray;
        }

        settings ??= new JsonMergeSettings();

        switch (settings.MergeArrayHandling)
        {
            case MergeArrayHandling.Concat:

                foreach (var item in jsonResource) 
                    jsonArray.Add(item.Clone());

                break;

            // 'Union' only for arrays of 'JsonValue's, otherwise acts as `Concat`,
            // this to prevent many expensive 'DeepEquals()' on more complex items.
            case MergeArrayHandling.Union:

                foreach (var item in jsonResource)
                {
                    // Only checking for existing 'JsonValue'.
                    if (item is JsonValue jsonValue && jsonArray.ContainsValue(jsonValue))
                        continue;

                    jsonArray.Add(item.Clone());
                }

                break;

            case MergeArrayHandling.Replace:

                if (jsonArray == jsonResource)
                    break;

                jsonArray.Clear();
                
                foreach (var item in jsonResource) 
                    jsonArray.Add(item.Clone());

                break;

            case MergeArrayHandling.Merge:

                for (var i = 0; i < jsonResource.Count; i++)
                {
                    var item = jsonResource[i];
                    if (item is null)
                    {
                        continue;
                    }

                    if (i < jsonArray.Count)
                    {
                        var existingItem = jsonArray[i];
                        if (existingItem is null)
                        {
                            jsonArray[i] = item.Clone();
                            continue;
                        }

                        if (existingItem is JsonObject jObject)
                        {
                            jObject.Merge(item, settings);
                            continue;
                        }

                        if (existingItem is JsonArray jArray)
                        {
                            jArray.Merge(item, settings);
                            continue;
                        }

                        if (existingItem is JsonValue || existingItem.GetValueKind() != item.GetValueKind())
                        {
                            if (item.GetValueKind() != JsonValueKind.Null ||
                                settings?.MergeNullValueHandling == MergeNullValueHandling.Merge)
                            {
                                jsonArray[i] = item.Clone();
                            }
                        }
                    }
                    else
                    {
                        jsonArray.Add(item.Clone());
                    }
                }

                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(settings), "Unexpected merge array handling when merging JSON.");
        }

        return jsonArray;
    }
}