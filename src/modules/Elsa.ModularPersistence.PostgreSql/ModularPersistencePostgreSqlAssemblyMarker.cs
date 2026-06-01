using Elsa.ModularPersistence.Relational;

namespace Elsa.ModularPersistence.PostgreSql;

/// <summary>
/// Marks the Elsa.ModularPersistence.PostgreSql assembly.
/// </summary>
public sealed class ModularPersistencePostgreSqlAssemblyMarker
{
    /// <summary>
    /// Gets the relational modular persistence marker type.
    /// </summary>
    public static Type RelationalMarkerType => typeof(ModularPersistenceRelationalAssemblyMarker);
}
