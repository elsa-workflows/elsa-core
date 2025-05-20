using System.Dynamic;
using Elsa.Scripting.JavaScript.Contracts;
using Elsa.Scripting.JavaScript.Extensions;
using Elsa.Workflows.LogPersistence;

namespace Elsa.Scripting.JavaScript.Services;

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
        this.RegisterType<byte[]>("Buffer");
        this.RegisterType<Stream>("Stream");
        this.RegisterType<Guid>("Guid");
        this.RegisterType<DateTime>("Date");
        this.RegisterType<DateTimeOffset>("Date");
        this.RegisterType<DateOnly>("Date");
        this.RegisterType<TimeOnly>("Date");
        this.RegisterType<IDictionary<string, object>>("ObjectDictionary");
        this.RegisterType<LogPersistenceMode>("LogPersistenceMode");
    }

    /// <inheritdoc />
    public void RegisterType(Type type, string alias)
    {
        _typeAliasDictionary[type] = alias;
    }

    /// <inheritdoc />
    public bool TryGetAlias(Type type, out string alias) => _typeAliasDictionary.TryGetValue(type, out alias!);
}