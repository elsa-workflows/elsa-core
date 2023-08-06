using System.Reflection;
using System.Text.Json;
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
        
        if (jsonTypeInfo.Type.GetConstructors(BindingFlags.Default).Length == 0)
        {
            // The type doesn't have public constructors
            jsonTypeInfo.CreateObject = () => Activator.CreateInstance(jsonTypeInfo.Type, true)!;
        }

        return jsonTypeInfo;
    }
}