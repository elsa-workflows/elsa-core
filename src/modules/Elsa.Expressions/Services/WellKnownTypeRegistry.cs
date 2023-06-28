using System.Dynamic;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Extensions;
using Elsa.Expressions.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Expressions.Services;

/// <inheritdoc />
public class WellKnownTypeRegistry : IWellKnownTypeRegistry
{
    private readonly IDictionary<string, Type> _aliasTypeDictionary = new Dictionary<string, Type>();
    private readonly IDictionary<Type, string> _typeAliasDictionary = new Dictionary<Type, string>();

    /// <summary>
    /// Creates a new instance of the <see cref="WellKnownTypeRegistry"/> class.
    /// </summary>
    /// <returns>The new instance.</returns>
    public static IWellKnownTypeRegistry CreateDefault()
    {
        var registry = new WellKnownTypeRegistry();
        registry.RegisterType<object>("Object");
        registry.RegisterType<string>("String");
        registry.RegisterType<bool>("Boolean");
        registry.RegisterType<short>("Int16");
        registry.RegisterType<int>("Int32");
        registry.RegisterType<long>("Int64");
        registry.RegisterType<decimal>("Decimal");
        registry.RegisterType<float>("Single");
        registry.RegisterType<double>("Double");
        registry.RegisterType<Guid>("Guid");
        registry.RegisterType<DateTime>("DateTime");
        registry.RegisterType<DateTimeOffset>("DateTimeOffset");
        registry.RegisterType<TimeSpan>("TimeSpan");
        registry.RegisterType<DateOnly>("DateOnly");
        registry.RegisterType<TimeOnly>("TimeOnly");
        registry.RegisterType<ExpandoObject>("JSON");
        registry.RegisterType<IDictionary<string, string>>("StringDictionary");
        registry.RegisterType<IDictionary<string, object>>("ObjectDictionary");
        return registry;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WellKnownTypeRegistry"/> class.
    /// </summary>
    public WellKnownTypeRegistry(IOptions<ExpressionOptions> expressionOptions)
    {
        foreach (var entry in expressionOptions.Value.AliasTypeDictionary)
            RegisterType(entry.Value, entry.Key);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WellKnownTypeRegistry"/> class.
    /// </summary>
    public WellKnownTypeRegistry()
    {
    }

    /// <inheritdoc />
    public void RegisterType(Type type, string alias)
    {
        _typeAliasDictionary[type] = alias;
        _aliasTypeDictionary[alias] = type;

        if (type.IsPrimitive || type.IsValueType && Nullable.GetUnderlyingType(type) == null)
        {
            var nullableType = typeof(Nullable<>).MakeGenericType(type);
            var nullableAlias = alias + "?";
            _typeAliasDictionary[nullableType] = nullableAlias;
            _aliasTypeDictionary[nullableAlias] = nullableType;
        }
    }

    /// <inheritdoc />
    public bool TryGetAlias(Type type, out string alias) => _typeAliasDictionary.TryGetValue(type, out alias!);

    /// <inheritdoc />
    public bool TryGetType(string alias, out Type type) => _aliasTypeDictionary.TryGetValue(alias, out type!);

    /// <inheritdoc />
    public IEnumerable<Type> ListTypes() => _typeAliasDictionary.Keys;
}