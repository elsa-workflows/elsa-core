using Elsa.Secrets.Management.Features;
using Elsa.Secrets.Persistence.EntityFrameworkCore;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Secrets.Persistence;

/// <summary>
/// Provides extensions to the <see cref="SecretManagementFeature"/> feature.
/// </summary>
[PublicAPI]
public static class Extensions
{
    /// <summary>
    /// Configures the <see cref="SecretManagementFeature"/> to use EF Core persistence providers.
    /// </summary>
    public static SecretManagementFeature UseEntityFrameworkCore(this SecretManagementFeature feature, Action<EFCoreSecretPersistenceFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}