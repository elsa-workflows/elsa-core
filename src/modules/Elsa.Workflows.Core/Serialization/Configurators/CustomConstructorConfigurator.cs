using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Elsa.Workflows.Core.Serialization.Configurators;

/// <summary>
/// Configures the contract resolver to add support for using non-default, private constructors for deserialization.
/// </summary>
public class CustomConstructorConfigurator : SerializationOptionsConfiguratorBase
{
    /// <inheritdoc />
    public override IEnumerable<Action<JsonTypeInfo>> GetModifiers()
    {
        // Set the default constructor for all types that have a default constructor.
        yield return jsonTypeInfo =>
        {
            if (jsonTypeInfo is not { Kind: JsonTypeInfoKind.Object, CreateObject: null })
                return;

            var defaultConstructor = GetDefaultConstructor(jsonTypeInfo.Type);
            if (defaultConstructor != null)
            {
                jsonTypeInfo.CreateObject = defaultConstructor;
            }
        };
    }
    
    private static Func<object>? GetDefaultConstructor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
    {
        foreach (var constructor in type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
        {
            // If we have a default constructor, use that one.
            if (!constructor.GetParameters().Any())
                return () => constructor.Invoke(default, Array.Empty<object>())!;

            // Else, find a constructor with the following signature: (string?, int?).
            // Check for a constructor with the following signature:
            // ctor(string, int) where string is decorated with [CallerFilePath] and int is decorated with [CallerLineNumber]
            var parameters = constructor.GetParameters();

            // Check parameter count
            if (parameters.Length != 2) continue;

            // Does the constructor have a [JsonConstructor] attribute?
            var isJsonConstructor = constructor.GetCustomAttribute<JsonConstructorAttribute>() != null;

            // Check first parameter type and attribute
            if (parameters[0].ParameterType != typeof(string) ||
                parameters[0].DefaultValue != null ||
                (parameters[0].GetCustomAttribute<CallerFilePathAttribute>() == null && !isJsonConstructor)) continue;

            // Check second parameter type and attribute
            if (parameters[1].ParameterType != typeof(int?) ||
                parameters[1].DefaultValue != null ||
                (parameters[1].GetCustomAttribute<CallerLineNumberAttribute>() == null && !isJsonConstructor)) continue;

            return () => constructor.Invoke(new object[] { null!, 0 });
        }

        return null;
    }
}