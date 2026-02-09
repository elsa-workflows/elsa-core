using CShells.Features;
using Elsa.Expressions.Liquid.Contracts;
using Elsa.Expressions.Liquid.Filters;
using Elsa.Expressions.Liquid.Handlers;
using Elsa.Expressions.Liquid.Options;
using Elsa.Expressions.Liquid.Providers;
using Elsa.Expressions.Liquid.Services;
using Elsa.Extensions;
using Fluid.Filters;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Expressions.Liquid.ShellFeatures;

/// <summary>
/// Configures Liquid functionality.
/// </summary>
[ShellFeature(
    DisplayName = "Liquid Expressions",
    Description = "Provides Liquid template expression evaluation capabilities for workflows",
    DependsOn = ["MemoryCache", "Mediator", "Expressions"])]
[UsedImplicitly]
public class LiquidFeature : IShellFeature
{
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

    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure(FluidOptions);

        services
            .AddHandlersFrom<ConfigureLiquidEngine>()
            .AddScoped<ILiquidTemplateManager, LiquidTemplateManager>()
            .AddScoped<LiquidParser>()
            .AddExpressionDescriptorProvider<LiquidExpressionDescriptorProvider>()
            .AddLiquidFilter<Base64Filter>("base64")
            .AddLiquidFilter<DictionaryKeysFilter>("keys");
    }
}

