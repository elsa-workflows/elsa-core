using Elsa.Secrets.Features;
using Elsa.Secrets.Persistence.EFCore.Features;

namespace Elsa.Secrets.Persistence.EFCore.Extensions;

/// <summary>
/// Provides extensions to the <see cref="SecretsFeature"/> feature.
/// </summary>
public static class SecretsFeatureExtensions
{
    /// <summary>
    /// Configures the <see cref="SecretsFeature"/> to use EF Core persistence.
    /// </summary>
    public static SecretsFeature UseEntityFrameworkCore(this SecretsFeature feature, Action<EFCoreSecretsPersistenceFeature>? configure = null)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}
