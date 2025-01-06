using System.Text.Json.Serialization;

namespace Elsa.Workflows.Serialization.ReferenceHandlers;

/// <summary>
/// A reference handler that can be used to resolve references across scopes.
/// </summary>
public class CrossScopedReferenceHandler : ReferenceHandler
{
    private static readonly AsyncLocal<CustomPreserveReferenceResolver> RootedResolverState = new();

    private CustomPreserveReferenceResolver RootedResolver
    {
        get
        {
            RootedResolverState.Value ??= new CustomPreserveReferenceResolver();
            return RootedResolverState.Value!;
        }
    }

    /// <inheritdoc />
    public override ReferenceResolver CreateResolver() => RootedResolver;

    /// <summary>
    /// Gets the reference resolver.
    /// </summary>
    /// <returns>The reference resolver.</returns>
    public ReferenceResolver GetResolver() => RootedResolver;
}