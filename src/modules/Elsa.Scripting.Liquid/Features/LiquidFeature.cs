using Elsa.Caching.Features;
using Elsa.Common.Features;
using Elsa.Expressions.Features;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Scripting.Liquid.Contracts;
using Elsa.Scripting.Liquid.Filters;
using Elsa.Scripting.Liquid.Handlers;
using Elsa.Scripting.Liquid.Options;
using Elsa.Scripting.Liquid.Providers;
using Elsa.Scripting.Liquid.Services;
using Fluid.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scripting.Liquid.Features;

/// <summary>
/// Configures liquid functionality.
/// </summary>
[DependsOn(typeof(MemoryCacheFeature))]
[DependsOn(typeof(MediatorFeature))]
[DependsOn(typeof(ExpressionsFeature))]
public class LiquidFeature : FeatureBase
{
    /// <inheritdoc />
    public LiquidFeature(IModule serviceConfiguration) : base(serviceConfiguration)
    {
    }

    /// <summary>
    /// Configures the Fluid options.
    /// </summary>
    public Action<FluidOptions> FluidOptions { get; set; } = options =>
    {
        options.ConfigureFilters = context => context.Options.Filters
            .WithArrayFilters()
            .WithStringFilters()
            .WithNumberFilters()
            .WithMiscFilters();
    };

    /// <inheritdoc />
    public override void Apply()
    {
        Services.Configure(FluidOptions);

        Services
            .AddHandlersFrom<ConfigureLiquidEngine>()
            .AddScoped<ILiquidTemplateManager, LiquidTemplateManager>()
            .AddScoped<LiquidParser>()
            .AddExpressionDescriptorProvider<LiquidExpressionDescriptorProvider>()
            .AddLiquidFilter<Base64Filter>("base64")
            .AddLiquidFilter<DictionaryKeysFilter>("keys")
        ;
    }
}