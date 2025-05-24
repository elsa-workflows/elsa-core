using Elsa.Expressions.PowerFx.Features;

namespace Elsa.Expressions.PowerFx.Extensions;

/// <summary>
/// Extension methods for <see cref="PowerFxFeature"/>.
/// </summary>
public static class PowerFxFeatureExtensions
{
    /// <summary>
    /// Configures the Power Fx feature.
    /// </summary>
    /// <param name="feature">The Power Fx feature to configure.</param>
    /// <returns>The Power Fx feature.</returns>
    public static PowerFxFeature Configure(this PowerFxFeature feature)
    {
        return feature;
    }
}