using System;
using Elsa.Options;
using Elsa.Runtime;
using Elsa.Scripting.Liquid.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ElsaServiceCollectionExtensions
    {
        public static IServiceCollection AddElsa(
            this IServiceCollection services,
            Action<ElsaOptionsBuilder>? configure = default) =>
            services
                .AddSingleton<IHostApplicationLifetime, ApplicationLifetime>()
                .AddStartupRunner()
                .AddElsaCore(configure)
                .AddJavaScriptExpressionEvaluator()
                .AddLiquidExpressionEvaluator();
    }
}