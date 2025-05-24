using Elsa.Caching.Features;
using Elsa.Common.Features;
using Elsa.Expressions.Features;
using Elsa.Expressions.PowerFx.Contracts;
using Elsa.Expressions.PowerFx.Providers;
using Elsa.Expressions.PowerFx.Services;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Expressions.PowerFx.Features;

/// <summary>
/// Installs Power Fx integration.
/// </summary>
[DependsOn(typeof(MediatorFeature))]
[DependsOn(typeof(ExpressionsFeature))]
[DependsOn(typeof(MemoryCacheFeature))]
public class PowerFxFeature : FeatureBase
{
    /// <inheritdoc />
    public PowerFxFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddFastEndpointsAssembly<PowerFxFeature>();
    }

    /// <inheritdoc />
    public override void Apply()
    {
        // Power Fx services.
        Services
            .AddScoped<IPowerFxEvaluator, PowerFxEvaluator>()
            .AddExpressionDescriptorProvider<PowerFxExpressionDescriptorProvider>();
        
        // Register notification handlers if needed
        Services.AddNotificationHandlersFrom<PowerFxFeature>();
    }
}