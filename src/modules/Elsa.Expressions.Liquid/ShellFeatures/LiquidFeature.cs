using CShells.Features;
using Elsa.Expressions.Liquid.Contracts;
using Elsa.Expressions.Liquid.Filters;
using Elsa.Expressions.Liquid.Handlers;
using Elsa.Expressions.Liquid.Options;
using Elsa.Expressions.Liquid.Providers;
using Elsa.Expressions.Liquid.Services;
using Elsa.Extensions;
using Fluid.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Expressions.Liquid.ShellFeatures;

/// <summary>
/// Configures liquid functionality.
/// </summary>
[ShellFeature(
    DisplayName = "Liquid Expressions",
    Description = "Enables Liquid template expression evaluation in workflows",
    DependsOn = ["MemoryCache", "Mediator", "Expressions"])]
public class LiquidFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure<FluidOptions>(options =>
        {
            options.ConfigureFilters = context => context.Options.Filters
                .WithArrayFilters()
                .WithStringFilters()
                .WithNumberFilters()
                .WithMiscFilters();
        });

        services
            .AddHandlersFrom<ConfigureLiquidEngine>()
            .AddScoped<ILiquidTemplateManager, LiquidTemplateManager>()
            .AddScoped<LiquidParser>()
            .AddExpressionDescriptorProvider<LiquidExpressionDescriptorProvider>()
            .AddLiquidFilter<Base64Filter>("base64")
            .AddLiquidFilter<DictionaryKeysFilter>("keys")
        ;
    }
}
