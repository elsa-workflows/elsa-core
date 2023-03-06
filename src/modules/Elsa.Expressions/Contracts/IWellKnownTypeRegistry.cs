namespace Elsa.Expressions.Contracts;

/// <summary>
/// A central repository of well known types.
/// </summary>
public interface IWellKnownTypeRegistry
{
    /// <summary>
    /// Register a type with an alias. 
    /// </summary>
    void RegisterType(Type type, string alias);
    
    /// <summary>
    /// Attempts to get an alias for the specified type.
    /// </summary>
    bool TryGetAlias(Type type, out string alias);
    
    /// <summary>
    /// Attempts to get the type associated with the specified alias.
    /// </summary>
    bool TryGetType(string alias, out Type type);

    /// <summary>
    /// Returns all registered types.
    /// </summary>
    IEnumerable<Type> ListTypes();
}