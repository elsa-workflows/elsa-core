using CShells.Features;
using Elsa.Caching.ShellFeatures;
using Elsa.Common.ShellFeatures;
using Elsa.Expressions.Liquid.Contracts;
using Elsa.Expressions.Liquid.Filters;
using Elsa.Expressions.Liquid.Handlers;
using Elsa.Expressions.Liquid.Options;
using Elsa.Expressions.Liquid.Providers;
using Elsa.Expressions.Liquid.Services;
using Elsa.Expressions.ShellFeatures;
using Elsa.Extensions;
using Elsa.Platform.PackageManifest.Generator.Hints;
using Fluid.Filters;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Expressions.Liquid.ShellFeatures;

/// <summary>
/// Configures Liquid functionality.
/// </summary>
[ManifestFeatureCategory(ManifestFeatureCategories.Expressions)]
[ManifestFeatureCategory(ManifestFeatureCategories.Scripting)]
[ShellFeature(
    DisplayName = "Liquid Expressions",
    Description = "Provides Liquid template expression evaluation capabilities for workflows",
    DependsOn = [typeof(MemoryCacheFeature), typeof(MediatorFeature), typeof(ExpressionsFeature)])]
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

