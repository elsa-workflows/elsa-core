namespace Elsa.Workflows;

/// <summary>
/// A registry of types that may be resolved from workflow JSON type identifiers.
/// </summary>
public interface IWorkflowJsonTypeRegistry
{
    /// <summary>
    /// Registers a type with an alias.
    /// </summary>
    void RegisterType(Type type, string alias);

    /// <summary>
    /// Attempts to get the preferred alias for the specified type.
    /// </summary>
    bool TryGetAlias(Type type, out string alias);

    /// <summary>
    /// Attempts to get the type associated with the specified alias or legacy name.
    /// </summary>
    bool TryGetType(string alias, out Type type);

    /// <summary>
    /// Returns all registered types.
    /// </summary>
    IEnumerable<Type> ListTypes();
}
