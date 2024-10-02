using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.ResourceManagement.Serialization.Settings;

namespace Elsa.ResourceManagement.Metadata.Builders;

public static class ResourceBuilderSettings
{
    /// <summary>
    /// Replace current value, even for null values, union arrays.
    /// </summary>
    public static readonly JsonMergeSettings JsonMergeSettings = new()
    {
        MergeArrayHandling = MergeArrayHandling.Union,
        MergeNullValueHandling = MergeNullValueHandling.Merge,
    };
        
    /// <summary>
    /// A Json serializer that ignores properties which have their default values.
    /// To be able to have a default value : use [DefaultValue(true)] on a class property for example.
    /// </summary>
    public static readonly JsonSerializerOptions IgnoreDefaultValuesSerializer = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        PropertyNameCaseInsensitive = true,
    };
}