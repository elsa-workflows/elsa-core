using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Mqtt.Features;

namespace Elsa.Extensions;

/// <summary>
/// Provides extension methods for configuring MQTT services.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Installs the MQTT module.
    /// </summary>
    public static IModule UseMqtt(this IModule module, Action<MqttFeature>? configure = null)
    {
        return module.Use(configure);
    }
}
