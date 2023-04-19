using Elsa.Features.Contracts;
using Elsa.Features.Models;

namespace Elsa.Features.Services;

/// <inheritdoc />
public class InstalledFeatureRegistry : IInstalledFeatureRegistry
{
    private readonly IDictionary<string, FeatureDescriptor> _descriptors = new Dictionary<string, FeatureDescriptor>();

    /// <inheritdoc />
    public void Add(FeatureDescriptor descriptor)
    {
        _descriptors[descriptor.Name] = descriptor;
    }

    /// <inheritdoc />
    public IEnumerable<FeatureDescriptor> List()
    {
        return _descriptors.Values;
    }
}