namespace Elsa.ModularPersistence.Descriptors;

/// <summary>
/// Describes how strongly a storage unit or index asks providers to materialize its physical shape.
/// </summary>
public enum PhysicalizationIntent
{
    /// <summary>
    /// Use the portable document-backed representation.
    /// </summary>
    PortableDocument = 0,

    /// <summary>
    /// Keep document storage as the source of truth while allowing providers to optimize declared indexes.
    /// </summary>
    OptimizedIndexes = 1,

    /// <summary>
    /// Allow providers to use native physical structures for the storage unit or index.
    /// </summary>
    NativePhysicalized = 2
}
