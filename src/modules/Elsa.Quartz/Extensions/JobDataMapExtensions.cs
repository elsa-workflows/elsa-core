using System.Text.Json;
using Quartz;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extension methods for <see cref="JobDataMap"/>.
/// </summary>
public static class JobDataMapExtensions
{
    /// <summary>
    /// Adds a key/value pair to the map if the value is not null or whitespace.
    /// </summary>
    public static JobDataMap AddIfNotEmpty(this JobDataMap map, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            map[key] = value;
        
        return map;
    }
    
    /// <summary>
    /// Adds a key/value pair to the map if the value is not null or empty.
    /// </summary>
    public static JobDataMap AddIfNotEmpty(this JobDataMap map, string key, IDictionary<string, object>? dictionary)
    {
        if (dictionary == null || !dictionary.Any())
            return map;
        
        var json = JsonSerializer.Serialize(dictionary);
        map[key] = json;
        
        return map;
    }
}