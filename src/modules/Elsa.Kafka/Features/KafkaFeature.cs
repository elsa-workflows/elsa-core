using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Kafka;

public class KafkaFeature(IModule module) : FeatureBase(module)
{
    private Action< KafkaOptions> _configureOptions = _ => { };
    
    public KafkaFeature ConfigureOptions(Action<KafkaOptions> configureOptions)
    {
        _configureOptions += configureOptions;
        return this;
    }
    
    public override void Apply()
    {
        Services.Configure(_configureOptions);
        Services.AddBackgroundTask<StartConsumersStartupTask>();
        Services.AddScoped<IConsumerDefinitionProvider, OptionsConsumerDefinitionProvider>();
    }
}