using CShells.FastEndpoints.Features;
using CShells.Features;
using Elsa.Expressions.Options;
using Elsa.Extensions;
using Elsa.Resilience.Endpoints.SimulateResponse;
using Elsa.Resilience.Entities;
using Elsa.Resilience.Modifiers;
using Elsa.Resilience.Options;
using Elsa.Resilience.Recorders;
using Elsa.Resilience.Serialization;
using Elsa.Resilience.StrategySources;
using Elsa.Workflows;
using Elsa.Workflows.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Elsa.Common.Serialization;
using Elsa.Platform.PackageManifest.Generator.Hints;

namespace Elsa.Resilience.ShellFeatures;

[ManifestFeatureCategory(ManifestFeatureCategories.Resilience)]
[ShellFeature(
    DisplayName = "Resilience",
    Description = "Provides workflow resilience strategies and retry attempt tracking")]
public class ResilienceFeature : IFastEndpointsShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure<ExpressionOptions>(options =>
        {
            options.AddTypeAlias<List<RetryAttemptRecord>>("RetryAttemptRecordList");
        });

        services.Configure<SerializationTypeOptions>(options =>
        {
            options.RegisterTypeAlias(typeof(List<RetryAttemptRecord>), "RetryAttemptRecordList");
        });
        
        services.AddOptions<ResilienceOptions>();
        services.AddOptions<SimulateResponseOptions>();
        services.TryAddSingleton(TimeProvider.System);

        services
            .AddSingleton<ResilienceStrategySerializer>()
            .AddSingleton<SimulateResponseSessionStore>()
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
            .AddHandlersFrom<ResilienceFeature>();

        // Register transient exception detection infrastructure
        services
            .AddSingleton<ITransientExceptionStrategy, DefaultTransientExceptionStrategy>()
            .AddSingleton<ITransientExceptionDetector, TransientExceptionDetector>();
    }
}
