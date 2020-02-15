using System;
using Elsa.Server.Core.Options;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddElsaServer(
            this IServiceCollection services, 
            Action<OptionsBuilder<ElsaServerOptions>> options = default)
        {
            var optionsBuilder = services.AddOptions<ElsaServerOptions>();
            options?.Invoke(optionsBuilder);

            return services;
        }
    }
}