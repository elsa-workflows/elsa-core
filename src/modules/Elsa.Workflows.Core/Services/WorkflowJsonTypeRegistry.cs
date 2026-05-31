using Elsa.Workflows.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Services;

/// <inheritdoc />
public class WorkflowJsonTypeRegistry : IWorkflowJsonTypeRegistry
{
    private readonly Dictionary<string, Type> _aliasTypeDictionary = new(StringComparer.Ordinal);
    private readonly Dictionary<Type, string> _typeAliasDictionary = new();

    /// <summary>
    /// Creates a default registry.
    /// </summary>
    public static IWorkflowJsonTypeRegistry CreateDefault()
    {
        return new WorkflowJsonTypeRegistry(Microsoft.Extensions.Options.Options.Create(new WorkflowJsonTypeOptions()));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowJsonTypeRegistry"/> class.
    /// </summary>
    public WorkflowJsonTypeRegistry(IOptions<WorkflowJsonTypeOptions> options)
    {
        foreach (var entry in options.Value.AliasTypeDictionary)
            RegisterTypeName(entry.Value, entry.Key);

        foreach (var entry in options.Value.TypeAliasDictionary)
            RegisterType(entry.Key, entry.Value);
    }

    /// <inheritdoc />
    public void RegisterType(Type type, string alias)
    {
        _typeAliasDictionary[type] = alias;
        RegisterTypeName(type, alias);
    }

    /// <inheritdoc />
    public bool TryGetAlias(Type type, out string alias) => _typeAliasDictionary.TryGetValue(type, out alias!);

    /// <inheritdoc />
    public bool TryGetType(string alias, out Type type) => _aliasTypeDictionary.TryGetValue(alias, out type!);

    /// <inheritdoc />
    public IEnumerable<Type> ListTypes() => _aliasTypeDictionary.Values.Distinct();

    private void RegisterTypeName(Type type, string alias)
    {
        _aliasTypeDictionary[alias] = type;

        if (type.IsPrimitive || type.IsValueType && Nullable.GetUnderlyingType(type) == null)
        {
            var nullableType = typeof(Nullable<>).MakeGenericType(type);
            _aliasTypeDictionary[$"{alias}?"] = nullableType;

            if (_typeAliasDictionary.ContainsKey(type))
                _typeAliasDictionary[nullableType] = $"{alias}?";
        }
    }
}
