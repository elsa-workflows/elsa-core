namespace Elsa.Management.Contracts;

public interface IWellKnownTypeRegistry
{
    void RegisterType(Type type, string alias);
    bool TryGetAlias(Type type, out string alias);
    bool TryGetType(string alias, out Type type);
}

public static class WellKnowTypeRegistryExtensions
{
    public static void RegisterType<T>(this IWellKnownTypeRegistry registry, string alias) => registry.RegisterType(typeof(T), alias);
}