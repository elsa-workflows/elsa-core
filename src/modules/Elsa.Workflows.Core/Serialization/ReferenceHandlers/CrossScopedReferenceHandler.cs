using System.Text.Json.Serialization;

namespace Elsa.Workflows.Core.Serialization.ReferenceHandlers;

/// <summary>
/// A reference handler that can be used to resolve references across scopes.
/// </summary>
public class CrossScopedReferenceHandler : ReferenceHandler
{
    /// <inheritdoc />
    public CrossScopedReferenceHandler() => Reset();
    private ReferenceResolver? _rootedResolver;

    /// <inheritdoc />
    public override ReferenceResolver CreateResolver() => _rootedResolver!;
    
    /// <summary>
    /// Resets the reference resolver.
    /// </summary>
    public void Reset() => _rootedResolver = new CustomPreserveReferenceResolver();
    
    /// <summary>
    /// Gets the reference resolver.
    /// </summary>
    /// <returns>The reference resolver.</returns>
    public ReferenceResolver GetResolver() => _rootedResolver!;
}