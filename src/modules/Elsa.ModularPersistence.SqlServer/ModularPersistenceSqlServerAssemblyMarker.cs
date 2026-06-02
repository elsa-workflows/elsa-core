using Elsa.ModularPersistence.Relational;

namespace Elsa.ModularPersistence.SqlServer;

/// <summary>
/// Marks the Elsa.ModularPersistence.SqlServer assembly.
/// </summary>
public sealed class ModularPersistenceSqlServerAssemblyMarker
{
    /// <summary>
    /// Gets the relational modular persistence marker type.
    /// </summary>
    public static Type RelationalMarkerType => typeof(ModularPersistenceRelationalAssemblyMarker);
}
