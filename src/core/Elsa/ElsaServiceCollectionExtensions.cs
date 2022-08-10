using System;
using Autofac;
using Elsa.Options;
using Elsa.Scripting.Liquid.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ElsaServiceCollectionExtensions
    {
        public static IServiceCollection AddElsaServices(
            this IServiceCollection services) =>
            services
                // This adds the required middleware to the ROOT CONTAINER and is required for multitenancy to work.
                .AddAutofacMultitenantRequestServices()
                .AddSingleton<IHostApplicationLifetime, ApplicationLifetime>()
                .AddElsaCore()
                .AddJavaScriptExpressionEvaluator()
                .AddLiquidExpressionEvaluator();

        public static ContainerBuilder AddElsaServices(
            this ContainerBuilder containerBuilder, IServiceCollection services,
            Action<ElsaOptionsBuilder>? configure = default) =>
            containerBuilder
                .ConfigureElsaServices(services, configure);
    }
}