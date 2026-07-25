using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;

namespace Elsa.ExternalAuthentication.Services;

public sealed class DefaultPermissionDescriptorRegistry(IEnumerable<IPermissionDescriptorProvider> providers) : IPermissionDescriptorRegistry
{
    private readonly IReadOnlyCollection<PermissionDescriptor> _descriptors = providers
        .SelectMany(x => x.GetDescriptors())
        .Where(x => !string.IsNullOrWhiteSpace(x.Name))
        .GroupBy(x => x.Name, StringComparer.Ordinal)
        .Select(x => x.First())
        .OrderBy(x => x.Name, StringComparer.Ordinal)
        .ToArray();

    public IReadOnlyCollection<PermissionDescriptor> List() => _descriptors;
}
