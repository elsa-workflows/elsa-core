using System.Dynamic;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Extensions;

namespace Elsa.Expressions.Services;

/// <inheritdoc />
public class WellKnownTypeRegistry : IWellKnownTypeRegistry
{
    private readonly IDictionary<string, Type> _aliasTypeDictionary = new Dictionary<string, Type>();
    private readonly IDictionary<Type, string> _typeAliasDictionary = new Dictionary<Type, string>();

    /// <summary>
    /// Constructor.
    /// </summary>
    public WellKnownTypeRegistry()
    {
        this.RegisterType<object>("Object");
        this.RegisterType<string>("String");
        this.RegisterType<bool>("Boolean");
        this.RegisterType<short>("Int16");
        this.RegisterType<int>("Int32");
        this.RegisterType<long>("Int64");
        this.RegisterType<decimal>("Decimal");
        this.RegisterType<float>("Single");
        this.RegisterType<double>("Double");
        this.RegisterType<Guid>("Guid");
        this.RegisterType<DateTime>("DateTime");
        this.RegisterType<DateTimeOffset>("DateTimeOffset");
        this.RegisterType<TimeSpan>("TimeSpan");
        this.RegisterType<DateOnly>("DateOnly");
        this.RegisterType<TimeOnly>("TimeOnly");
        this.RegisterType<ExpandoObject>("JSON");
        this.RegisterType<IDictionary<string, string>>("StringDictionary");
        this.RegisterType<IDictionary<string, object>>("ObjectDictionary");
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