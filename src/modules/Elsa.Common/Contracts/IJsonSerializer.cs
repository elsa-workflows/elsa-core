using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Elsa.Common.Contracts;

/// <summary>
/// Provides JSON serialization services.
/// </summary>
public interface IJsonSerializer
{
    /// <summary>
    /// Returns the serializer options.
    /// </summary>
    JsonSerializerOptions GetOptions();

    /// <summary>
    /// Applies the specified options.
    /// </summary>
    void ApplyOptions(JsonSerializerOptions options);

    /// <summary>
    /// Serializes the specified value.
    /// </summary>
    [RequiresUnreferencedCode("The type is not known at compile time.")]
    string Serialize(object value);

    /// <summary>
    /// Serializes the specified value.
    /// </summary>
    [RequiresUnreferencedCode("The type is not known at compile time.")]
    string Serialize(object value, Type type);

    /// <summary>
    /// Serializes the specified value.
    /// </summary>
    string Serialize<T>(T value);

    /// <summary>
    /// Deserializes the specified JSON.
    /// </summary>
    [RequiresUnreferencedCode("The type is not known at compile time.")]
    object Deserialize(string json);

    /// <summary>
    /// Deserializes the specified JSON.
    /// </summary>
    [RequiresUnreferencedCode("The type is not known at compile time.")]
    object Deserialize(string json, Type type);
    
    /// <summary>
    /// Deserializes the specified JSON.
    /// </summary>
    T Deserialize<T>(string json);
}