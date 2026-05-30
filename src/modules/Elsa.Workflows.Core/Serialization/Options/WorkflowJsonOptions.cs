using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Extensions;

namespace Elsa.Workflows.Serialization.Options;

/// <summary>
/// Options used by workflow JSON serializers to map stable type aliases to CLR types.
/// </summary>
public class WorkflowJsonOptions
{
    private readonly Dictionary<string, Type> _typesByAlias = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Type, string> _aliasesByType = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowJsonOptions"/> class.
    /// </summary>
    public WorkflowJsonOptions()
    {
        RegisterDefaultTypeAliases();
    }

    /// <summary>
    /// Gets or sets a value indicating whether unrestricted CLR type names can be resolved for backwards compatibility.
    /// </summary>
    public bool AllowLegacyClrTypeNames { get; set; } = true;

    /// <summary>
    /// Registers a type alias.
    /// </summary>
    public WorkflowJsonOptions RegisterTypeAlias(Type type, string alias)
    {
        _typesByAlias[alias] = type;
        _aliasesByType[type] = alias;

        if (type.IsPrimitive || type.IsValueType && Nullable.GetUnderlyingType(type) == null)
        {
            var nullableType = typeof(Nullable<>).MakeGenericType(type);
            var nullableAlias = alias + "?";
            _typesByAlias[nullableAlias] = nullableType;
            _aliasesByType[nullableType] = nullableAlias;
        }

        return this;
    }

    /// <summary>
    /// Attempts to resolve the specified alias to a type.
    /// </summary>
    public bool TryGetType(string alias, out Type type) => _typesByAlias.TryGetValue(alias, out type!);

    /// <summary>
    /// Attempts to get the primary alias registered for the specified type.
    /// </summary>
    public bool TryGetAlias(Type type, out string alias) => _aliasesByType.TryGetValue(type, out alias!);

    /// <summary>
    /// Lists all registered types.
    /// </summary>
    public IEnumerable<Type> ListTypes() => _typesByAlias.Values.Distinct();

    private void RegisterDefaultTypeAliases()
    {
        this.AddTypeAlias<short>("Int16");
        this.AddTypeAlias<int>("Int32");
        this.AddTypeAlias<long>("Int64");
        this.AddTypeAlias<long>("Long");
        this.AddTypeAlias<float>("Single");
        this.AddTypeAlias<object>("Object");
        this.AddTypeAlias<string>("String");
        this.AddTypeAlias<bool>("Boolean");
        this.AddTypeAlias<decimal>("Decimal");
        this.AddTypeAlias<double>("Double");
        this.AddTypeAlias<byte[]>("ByteArray");
        this.AddTypeAlias<Guid>();
        this.AddTypeAlias<DateTime>();
        this.AddTypeAlias<DateTimeOffset>();
        this.AddTypeAlias<TimeSpan>();
        this.AddTypeAlias<Stream>();
        this.AddTypeAlias<ExpandoObject>("JSON");
        this.AddTypeAlias<JsonElement>();
        this.AddTypeAlias<JsonNode>();
        this.AddTypeAlias<JsonObject>();
        this.AddTypeAlias<JsonArray>();
        this.AddTypeAlias<IDictionary<string, string>>("StringDictionary");
        this.AddTypeAlias<IDictionary<string, object>>("ObjectDictionary");
        this.AddTypeAlias<Dictionary<string, string>>("StringMap");
        this.AddTypeAlias<Dictionary<string, object>>("ObjectMap");
    }
}
