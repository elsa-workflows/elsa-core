using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Mqtt.Contracts;
using Elsa.Mqtt.Options;
using Elsa.Mqtt.Services;
using Elsa.Mqtt.Tasks;
using Elsa.Mqtt.UIHints;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Mqtt.Features;

/// <summary>
/// Setup MQTT features.
/// </summary>
public class MqttFeature : FeatureBase
{
    /// <inheritdoc />
    public MqttFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// Set a callback to configure <see cref="MqttOptions"/>.
    /// </summary>
    public Action<MqttOptions> ConfigureOptions { get; set; } = _ => { };

    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddActivitiesFrom<MqttFeature>();
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services
            .Configure(ConfigureOptions)
            .AddSingleton<IMqttClientFactory, MqttClientFactory>()
            .AddSingleton<IMqttSubscriberManager, MqttSubscriberManager>()
            .AddBackgroundTask<StartMqttSubscriptionsTask>()
            .AddScoped<IPropertyUIHandler, MqttConnectionDropdownOptionsProvider>()
            .AddScoped<IPropertyUIHandler, MqttQosLevelDropdownOptionsProvider>()
            .AddHandlersFrom<MqttFeature>();
    }
}