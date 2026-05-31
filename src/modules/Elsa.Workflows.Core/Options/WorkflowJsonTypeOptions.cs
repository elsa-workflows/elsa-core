using System.Collections.ObjectModel;
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Extensions;

namespace Elsa.Workflows.Options;

/// <summary>
/// Options for workflow JSON type identifiers.
/// </summary>
public class WorkflowJsonTypeOptions
{
    private readonly IDictionary<string, Type> _aliasTypeDictionary = new Dictionary<string, Type>(StringComparer.Ordinal);
    private readonly IDictionary<Type, string> _typeAliasDictionary = new Dictionary<Type, string>();

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowJsonTypeOptions"/> class.
    /// </summary>
    public WorkflowJsonTypeOptions()
    {
        AliasTypeDictionary = new ReadOnlyDictionary<string, Type>(_aliasTypeDictionary);
        TypeAliasDictionary = new ReadOnlyDictionary<Type, string>(_typeAliasDictionary);

        RegisterTypeAlias(typeof(short), "Int16");
        RegisterTypeAlias(typeof(int), "Int32");
        RegisterTypeAlias(typeof(long), "Int64");
        RegisterLegacyTypeName(typeof(long), "Long");
        RegisterTypeAlias(typeof(float), "Single");
        RegisterTypeAlias(typeof(object), "Object");
        RegisterTypeAlias(typeof(string), "String");
        RegisterTypeAlias(typeof(bool), "Boolean");
        RegisterTypeAlias(typeof(decimal), "Decimal");
        RegisterTypeAlias(typeof(double), "Double");
        RegisterTypeAlias(typeof(byte[]), "ByteArray");
        RegisterTypeAlias(typeof(Guid), nameof(Guid));
        RegisterTypeAlias(typeof(DateTime), nameof(DateTime));
        RegisterTypeAlias(typeof(DateTimeOffset), nameof(DateTimeOffset));
        RegisterTypeAlias(typeof(TimeSpan), nameof(TimeSpan));
        RegisterTypeAlias(typeof(Stream), nameof(Stream));
        RegisterTypeAlias(typeof(ExpandoObject), "JSON");
        RegisterTypeAlias(typeof(JsonElement), nameof(JsonElement));
        RegisterTypeAlias(typeof(JsonNode), nameof(JsonNode));
        RegisterTypeAlias(typeof(JsonObject), nameof(JsonObject));
        RegisterTypeAlias(typeof(JsonArray), nameof(JsonArray));
        RegisterTypeAlias(typeof(IDictionary<string, string>), "StringDictionary");
        RegisterTypeAlias(typeof(IDictionary<string, object>), "ObjectDictionary");
        RegisterTypeAlias(typeof(Dictionary<string, string>), "StringMap");
        RegisterTypeAlias(typeof(Dictionary<string, object>), "ObjectMap");
    }

    /// <summary>
    /// Gets aliases and legacy names keyed by identifier.
    /// </summary>
    public IDictionary<string, Type> AliasTypeDictionary { get; }

    /// <summary>
    /// Gets preferred aliases keyed by type.
    /// </summary>
    public IDictionary<Type, string> TypeAliasDictionary { get; }

    /// <summary>
    /// Registers a preferred workflow JSON alias.
    /// </summary>
    public WorkflowJsonTypeOptions RegisterTypeAlias(Type type, string alias)
    {
        _aliasTypeDictionary[alias] = type;
        _typeAliasDictionary[type] = alias;
        return this;
    }

    /// <summary>
    /// Registers a legacy workflow JSON identifier for compatibility reads.
    /// </summary>
    public WorkflowJsonTypeOptions RegisterLegacyTypeName(Type type, string typeName)
    {
        _aliasTypeDictionary[typeName] = type;
        return this;
    }

    /// <summary>
    /// Registers the type's simple assembly-qualified name as a legacy workflow JSON identifier.
    /// </summary>
    public WorkflowJsonTypeOptions RegisterLegacySimpleAssemblyQualifiedName(Type type) => RegisterLegacyTypeName(type, type.GetSimpleAssemblyQualifiedName());
}
