using Elsa.Scripting.Liquid.Contracts;
using Elsa.Scripting.Liquid.Options;
using Fluid;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

internal static class TemplateContextExtensions
{
    internal static void AddFilters(this TemplateContext templateContext, FluidOptions options, IServiceProvider services)
    {
        foreach (var (key, value) in options.FilterRegistrations)
        {
            templateContext.Options.Filters.AddFilter(key, (input, arguments, ctx) =>
            {
                var filter = (ILiquidFilter)services.GetRequiredService(value);
                return filter.ProcessAsync(input, arguments, ctx);
            });
        }
        options.ConfigureFilters(templateContext);
    }
}