using Elsa.Liquid.Options;
using Elsa.Liquid.Services;
using Fluid;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Liquid.Extensions
{
    public static class TemplateContextExtensions
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
        }
    }
}