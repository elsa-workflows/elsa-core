using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Elsa.Workflows.Core.Serialization;

/// <summary>
/// A custom JSON type info resolver that allows private constructors to be used when deserializing JSON.
/// </summary>
public class PrivateConstructorContractResolver : DefaultJsonTypeInfoResolver
{
    /// <inheritdoc />
    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        var jsonTypeInfo = base.GetTypeInfo(type, options);

        if (jsonTypeInfo is not { Kind: JsonTypeInfoKind.Object, CreateObject: null }) 
            return jsonTypeInfo;
        
        if (HasPrivateJsonConstructor(jsonTypeInfo.Type))
        {
            // The type doesn't have public constructors
            jsonTypeInfo.CreateObject = () => Activator.CreateInstance(jsonTypeInfo.Type, true)!;
        }

        return jsonTypeInfo;
    }

    private static bool HasPrivateJsonConstructor(Type type) => type
        .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
        .Any(x => (x.IsPrivate || x.IsAssembly || x.IsFamily) && !x.GetParameters().Any() && x.GetCustomAttribute<JsonConstructorAttribute>() != null);
}