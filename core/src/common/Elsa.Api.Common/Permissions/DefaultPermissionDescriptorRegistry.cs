using Elsa.Authorization;

namespace Elsa.Permissions;

/// <inheritdoc />
public sealed class DefaultPermissionDescriptorRegistry(IEnumerable<IPermissionDescriptorProvider> providers) : IPermissionDescriptorRegistry
{
    private readonly IReadOnlyCollection<PermissionDescriptor> _descriptors = providers
        .SelectMany(x => x.GetDescriptors())
        .Where(x => !string.IsNullOrWhiteSpace(x.Resource))
        .GroupBy(x => x.Resource, StringComparer.Ordinal)
        .Select(x => x.First())
        .OrderBy(x => x.Resource, StringComparer.Ordinal)
        .ToArray();

    /// <inheritdoc />
    public IReadOnlyCollection<PermissionDescriptor> List() => _descriptors;

    /// <inheritdoc />
    public PermissionDescriptor? Find(string resource) =>
        _descriptors.FirstOrDefault(x => string.Equals(x.Resource, resource, StringComparison.Ordinal));

    /// <inheritdoc />
    public IReadOnlyCollection<string> Reach(string resourcePattern) =>
        _descriptors
            .Where(x => PermissionMatcher.ResourceMatches(resourcePattern, x.Resource))
            .Select(x => x.Resource)
            .ToArray();
}
