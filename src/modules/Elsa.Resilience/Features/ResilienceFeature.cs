using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Resilience.Modifiers;
using Elsa.Resilience.Options;
using Elsa.Resilience.Providers;
using Elsa.Resilience.Serialization;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Resilience.Features;

public class ResilienceFeature(IModule module) : FeatureBase(module)
{
    public ResilienceFeature AddResiliencyStrategyType<T>() where T : IResilienceStrategy
    {
        return AddResiliencyStrategyType(typeof(T));
    }

    public ResilienceFeature AddResiliencyStrategyType(Type strategyType)
    {
        Services.Configure<ResilienceOptions>(options => options.StrategyTypes.Add(strategyType));
        return this;
    }

    public override void Configure()
    {
        Module.AddFastEndpointsAssembly<ResilienceFeature>();
    }

    public override void Apply()
    {
        Services.AddOptions<ResilienceOptions>();

        Services
            .AddSingleton<ResilienceStrategySerializer>()
            .AddSingleton<IActivityDescriptorModifier, ResilientActivityDescriptorModifier>()
            .AddScoped<IResilienceStrategyCatalog, ResilienceStrategyCatalog>()
            .AddScoped<IResilienceStrategyConfigEvaluator, ResilienceStrategyConfigEvaluator>()
            .AddScoped<IResilienceStrategySource, ConfigurationResilienceStrategySource>();
    }
}