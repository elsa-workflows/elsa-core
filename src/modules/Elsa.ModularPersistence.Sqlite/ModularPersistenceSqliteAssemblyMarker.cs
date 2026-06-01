using Elsa.ModularPersistence.Relational;

namespace Elsa.ModularPersistence.Sqlite;

/// <summary>
/// Marks the Elsa.ModularPersistence.Sqlite assembly.
/// </summary>
public sealed class ModularPersistenceSqliteAssemblyMarker
{
    /// <summary>
    /// Gets the relational modular persistence marker type.
    /// </summary>
    public static Type RelationalMarkerType => typeof(ModularPersistenceRelationalAssemblyMarker);
}
