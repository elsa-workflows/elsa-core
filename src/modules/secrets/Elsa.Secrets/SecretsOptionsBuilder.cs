using Elsa.Caching;
using Elsa.Secrets.Options;
using Elsa.Secrets.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Elsa.Secrets
{
    public class SecretsOptionsBuilder
    {
        public SecretsOptionsBuilder(IServiceCollection services)
        {
            SecretsOptions = new SecretsOptions();
            Services = services;
            services.TryAddSingleton<ICacheSignal, CacheSignal>();
        }

        public IServiceCollection Services { get; }
        public SecretsOptions SecretsOptions { get; }

        public SecretsOptionsBuilder UseSecretsStore(Func<IServiceProvider, ISecretsStore> factory)
        {
            SecretsOptions.SecretsStoreFactory = factory;
            return this;
        }
    }
}