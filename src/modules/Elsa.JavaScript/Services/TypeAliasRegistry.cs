using System.Dynamic;
using Elsa.JavaScript.Contracts;
using Elsa.JavaScript.Extensions;

namespace Elsa.JavaScript.Services;

/// <inheritdoc />
public class TypeAliasRegistry : ITypeAliasRegistry
{
    private readonly IDictionary<Type, string> _typeAliasDictionary = new Dictionary<Type, string>();

    /// <summary>
    /// Constructor.
    /// </summary>
    public TypeAliasRegistry()
    {
        this.RegisterType<object>("any");
        this.RegisterType<ExpandoObject>("any");
        this.RegisterType<string>("string");
        this.RegisterType<bool>("boolean");
        this.RegisterType<short>("number");
        this.RegisterType<int>("number");
        this.RegisterType<long>("number");
        this.RegisterType<decimal>("Decimal");
        this.RegisterType<float>("Single");
        this.RegisterType<double>("Double");
        this.RegisterType<DateTime>("Date");
        this.RegisterType<DateTimeOffset>("Date");
        this.RegisterType<DateOnly>("Date");
        this.RegisterType<TimeOnly>("Date");
    }

    /// <inheritdoc />
    public void RegisterType(Type type, string alias)
    {
        _typeAliasDictionary[type] = alias;
    }

    /// <inheritdoc />
    public bool TryGetAlias(Type type, out string alias) => _typeAliasDictionary.TryGetValue(type, out alias!);
}