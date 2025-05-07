using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Resilience.Options;
using Elsa.Resilience.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Resilience.Features;

public class ResilienceFeature(IModule module) : FeatureBase(module)
{
    public ResilienceFeature AddResilienceStrategy(IResilienceStrategy strategy)
    {
        Services.Configure<ResilienceStrategiesOptions>(options => options.ResilienceStrategies.Add(strategy));
        return this;
    }

    public override void Apply()
    {
        Services.AddOptions<ResilienceStrategiesOptions>();
        Services.AddScoped<IResilienceStrategyProvider, ConfigurationResilienceStrategyProvider>();
        Services.AddScoped<IResilienceService, ResilienceService>();
    }
}