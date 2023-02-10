namespace Elsa.JavaScript.Contracts;

/// <summary>
/// A central registry of type aliases.
/// </summary>
public interface ITypeAliasRegistry
{
    /// <summary>
    /// Register a type with an alias. 
    /// </summary>
    void RegisterType(Type type, string alias);
    
    /// <summary>
    /// Attempts to get an alias for the specified type.
    /// </summary>
    bool TryGetAlias(Type type, out string alias);
}