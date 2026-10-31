using CShells.Features;
using Elsa.Features.Contracts;
using Elsa.Features.Models;

namespace Elsa.Features.Services;

/// <summary>
/// Shell-based implementation of <see cref="IInstalledFeatureProvider"/> that reads from CShells feature descriptors.
/// </summary>
/// <remarks>
/// This implementation automatically discovers features from the shell's feature descriptors,
/// mapping them to Elsa's <see cref="FeatureDescriptor"/> model. Features are read from the
/// shell's DI container where they were registered during shell initialization.
/// </remarks>
public class ShellInstalledFeatureProvider : IInstalledFeatureProvider
{
    private readonly IEnumerable<ShellFeatureDescriptor> _shellFeatures;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShellInstalledFeatureProvider"/> class.
    /// </summary>
    public ShellInstalledFeatureProvider(IEnumerable<ShellFeatureDescriptor> shellFeatures)
    {
        _shellFeatures = shellFeatures;
    }

    /// <inheritdoc />
    public IEnumerable<FeatureDescriptor> List()
    {
        return _shellFeatures
            .Where(sf => sf.StartupType != null)
            .Select(MapToElsaFeatureDescriptor);
    }

    /// <inheritdoc />
    public FeatureDescriptor? Find(string fullName)
    {
        var shellFeature = _shellFeatures
            .Where(sf => sf.StartupType != null)
            .FirstOrDefault(sf => MapToFullName(sf) == fullName);

        return shellFeature != null ? MapToElsaFeatureDescriptor(shellFeature) : null;
    }

    private static FeatureDescriptor MapToElsaFeatureDescriptor(ShellFeatureDescriptor shell)
    {
        // Extract namespace and name from shell feature
        var type = shell.StartupType!;
        var ns = type.Namespace ?? "Unknown";
        var name = shell.Id;

        // Try to get display name and description from metadata
        // These can be set via ShellFeatureAttribute properties
        var displayName = shell.Metadata.TryGetValue("DisplayName", out var dn)
            ? dn.ToString() ?? name
            : name;

        var description = shell.Metadata.TryGetValue("Description", out var desc)
            ? desc.ToString() ?? string.Empty
            : string.Empty;

        return new FeatureDescriptor(name, ns, displayName, description);
    }

    private static string MapToFullName(ShellFeatureDescriptor shell)
    {
        var ns = shell.StartupType?.Namespace ?? "Unknown";
        return $"{ns}.{shell.Id}";
    }
}
