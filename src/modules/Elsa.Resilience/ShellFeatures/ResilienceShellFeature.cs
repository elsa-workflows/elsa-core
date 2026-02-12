using CShells.FastEndpoints.Features;
using CShells.Features;
using Elsa.Expressions.Options;
using Elsa.Extensions;
using Elsa.Resilience.Entities;
using Elsa.Resilience.Modifiers;
using Elsa.Resilience.Options;
using Elsa.Resilience.Recorders;
using Elsa.Resilience.Serialization;
using Elsa.Resilience.StrategySources;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Resilience.ShellFeatures;

[ShellFeature]
public class ResilienceShellFeature : IFastEndpointsShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure<ExpressionOptions>(options =>
        {
            options.AddTypeAlias<List<RetryAttemptRecord>>("RetryAttemptRecordList");
        });
        
        services.AddOptions<ResilienceOptions>();

        services
            .AddSingleton<ResilienceStrategySerializer>()
            .AddSingleton<IActivityDescriptorModifier, ResilientActivityDescriptorModifier>()
            .AddScoped<IResilienceStrategyCatalog, ResilienceStrategyCatalog>()
            .AddScoped<IResilienceStrategyConfigEvaluator, ResilienceStrategyConfigEvaluator>()
            .AddScoped<IResilientActivityInvoker, ResilientActivityInvoker>()
            .AddScoped<IResilienceStrategySource, ConfigurationResilienceStrategySource>()
            .AddSingleton(VoidRetryAttemptRecorder.Instance)
            .AddSingleton(VoidRetryAttemptReader.Instance)
            .AddScoped<IRetryAttemptRecorder, ActivityExecutionContextRetryAttemptRecorder>()
            .AddScoped<IRetryAttemptReader, ActivityExecutionContextRetryAttemptReader>()
            .AddScoped<ActivityExecutionContextRetryAttemptReader>()
            .AddHandlersFrom<ResilienceShellFeature>();

        // Register transient exception detection infrastructure
        services
            .AddSingleton<ITransientExceptionStrategy, DefaultTransientExceptionStrategy>()
            .AddSingleton<ITransientExceptionDetector, TransientExceptionDetector>();
    }
}