using System;
using Elsa;
using Elsa.Runtime;
using Elsa.Scripting.Liquid.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ElsaServiceCollectionExtensions
    {
        public static IServiceCollection AddElsa(
            this IServiceCollection services,
            Action<ElsaOptions>? configure = default) =>
            services
                .AddElsaCore(configure)
                .AddStartupRunner()
                .AddJavaScriptExpressionEvaluator()
                .AddLiquidExpressionEvaluator();
    }
}