using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Resilience.Modifiers;
using Elsa.Resilience.Options;
using Elsa.Resilience.Recorders;
using Elsa.Resilience.Serialization;
using Elsa.Resilience.StrategySources;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Resilience.Features;

public class ResilienceFeature(IModule module) : FeatureBase(module)
{
    private Func<IServiceProvider, IRetryAttemptRecorder> _retryAttemptRecorder = sp => sp.GetRequiredService<ActivityExecutionContextRetryAttemptRecorder>();
    
    public ResilienceFeature AddResilienceStrategyType<T>() where T : IResilienceStrategy
    {
        return AddResilienceStrategyType(typeof(T));
    }

    public ResilienceFeature AddResilienceStrategyType(Type strategyType)
    {
        Services.Configure<ResilienceOptions>(options => options.StrategyTypes.Add(strategyType));
        return this;
    }

    public ResilienceFeature WithActivityExecutionContextRetryAttemptRecorder() => WithRetryAttemptRecorder<ActivityExecutionContextRetryAttemptRecorder>();
    public ResilienceFeature WithVoidRetryAttemptRecorder() => WithRetryAttemptRecorder<VoidRetryAttemptRecorder>();
    public ResilienceFeature WithRetryAttemptRecorder<T>() => WithRetryAttemptRecorder(sp => (IRetryAttemptRecorder)ActivatorUtilities.CreateInstance<T>(sp)!);

    public ResilienceFeature WithRetryAttemptRecorder(Func<IServiceProvider, IRetryAttemptRecorder> recorder)
    {
        _retryAttemptRecorder = recorder;
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
            .AddScoped<IResilientActivityInvoker, ResilientActivityInvoker>()
            .AddScoped<IResilienceStrategySource, ConfigurationResilienceStrategySource>()
            .AddSingleton(VoidRetryAttemptRecorder.Instance)
            .AddScoped<ActivityExecutionContextRetryAttemptRecorder>()
            .AddScoped(_retryAttemptRecorder);
    }
}