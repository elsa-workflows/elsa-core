namespace Elsa.Common.Serialization;

public static class TypeAliasRegistry
{
    public static Dictionary<string, Type> TypeAliases { get; } = new();

    public static void RegisterAlias(string alias, Type type) => TypeAliases[alias] = type;

    public static Type? GetType(string alias) => TypeAliases.GetValueOrDefault(alias);
}