using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Elsa.Common.Contracts;

/// <summary>
/// Configures <see cref="JsonSerializerOptions"/> objects.
/// </summary>
public interface ISerializationOptionsConfigurator
{
    /// <summary>
    /// Configures the specified <see cref="JsonSerializerOptions"/> object.
    /// </summary>
    void Configure(JsonSerializerOptions options);
    
    /// <summary>
    /// Returns a set of modifiers to be added to the type info resolver used by the serializer.
    /// </summary>
    IEnumerable<Action<JsonTypeInfo>> GetModifiers();
}