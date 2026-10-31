using Elsa.Features.Contracts;
using Elsa.Features.Models;

namespace Elsa.Features.Services;

/// <summary>
/// Legacy implementation of <see cref="IInstalledFeatureProvider"/> that delegates to <see cref="IInstalledFeatureRegistry"/>.
/// </summary>
/// <remarks>
/// This implementation is used when features are manually registered using the legacy feature system.
/// For shell-based feature discovery, use <c>ShellInstalledFeatureProvider</c> instead.
/// </remarks>
public class InstalledFeatureProvider : IInstalledFeatureProvider
{
    private readonly IInstalledFeatureRegistry _registry;

    /// <summary>
    /// Initializes a new instance of the <see cref="InstalledFeatureProvider"/> class.
    /// </summary>
    public InstalledFeatureProvider(IInstalledFeatureRegistry registry)
    {
        _registry = registry;
    }

    /// <inheritdoc />
    public IEnumerable<FeatureDescriptor> List() => _registry.List();

    /// <inheritdoc />
    public FeatureDescriptor? Find(string fullName) => _registry.Find(fullName);
}
