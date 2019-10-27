using System;
using Elsa.Scripting.Liquid.Options;
using Elsa.Scripting.Liquid.Services;
using Fluid;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scripting.Liquid.Extensions
{
    public static class TemplateContextExtensions
    {
        internal static void AddAsyncFilters(this TemplateContext templateContext, LiquidOptions options, IServiceProvider services)
        {
            foreach (var registration in options.FilterRegistrations)
            {
                templateContext.Filters.AddAsyncFilter(registration.Key, (input, arguments, ctx) =>
                {
                    var filter = (ILiquidFilter) services.GetRequiredService(registration.Value);
                    return filter.ProcessAsync(input, arguments, ctx);
                });
            }
        }
    }
}