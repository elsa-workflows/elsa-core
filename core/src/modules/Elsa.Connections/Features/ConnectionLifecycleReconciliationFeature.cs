using Elsa.Connections.Contracts;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Elsa.Connections.Features;

/// <summary>Installs the opt-in hosted credential lifecycle reconciler.</summary>
[DependsOn(typeof(ConnectionsFeature))]
public sealed class ConnectionLifecycleReconciliationFeature(IModule module) : FeatureBase(module)
{
    public override void Apply()
    {
        Services.TryAddSingleton<IConnectionLifecycleScopeProvider, EmptyConnectionLifecycleScopeProvider>();
        Services.AddOptions<ConnectionLifecycleReconciliationOptions>()
            .Validate(options => options.Interval > TimeSpan.Zero, "Interval must be positive.")
            .Validate(options => options.BatchSize is >= 1 and <= 500, "Batch size must be between 1 and 500.")
            .Validate(options => options.MaxConcurrency is >= 1 and <= 64, "Maximum concurrency must be between 1 and 64.")
            .Validate(options => options.RetryBackoff > TimeSpan.Zero && options.MaxRetryBackoff >= options.RetryBackoff,
                "Retry backoff values are invalid.")
            .ValidateOnStart();
        Services.AddSingleton<IHostedService, ConnectionLifecycleReconciliationWorker>();
    }
}
