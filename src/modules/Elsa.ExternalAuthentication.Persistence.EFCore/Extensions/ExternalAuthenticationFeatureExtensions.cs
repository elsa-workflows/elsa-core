using Elsa.ExternalAuthentication.Features;
using Elsa.ExternalAuthentication.Persistence.EFCore.Features;

namespace Elsa.ExternalAuthentication.Persistence.EFCore.Extensions;

/// <summary>
/// Provides extensions to the <see cref="ExternalAuthenticationFeature"/> feature.
/// </summary>
public static class ExternalAuthenticationFeatureExtensions
{
    /// <summary>
    /// Configures the <see cref="ExternalAuthenticationFeature"/> to use EF Core persistence.
    /// </summary>
    public static ExternalAuthenticationFeature UseEntityFrameworkCore(this ExternalAuthenticationFeature feature, Action<EFCoreExternalAuthenticationPersistenceFeature>? configure = null)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}
