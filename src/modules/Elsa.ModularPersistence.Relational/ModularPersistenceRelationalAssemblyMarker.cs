using Elsa.ModularPersistence;

namespace Elsa.ModularPersistence.Relational;

/// <summary>
/// Marks the Elsa.ModularPersistence.Relational assembly.
/// </summary>
public sealed class ModularPersistenceRelationalAssemblyMarker
{
    /// <summary>
    /// Gets the core modular persistence marker type.
    /// </summary>
    public static Type CoreMarkerType => typeof(ModularPersistenceAssemblyMarker);
}
