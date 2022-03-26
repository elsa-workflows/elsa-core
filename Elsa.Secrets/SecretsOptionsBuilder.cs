using Elsa.Caching;
using Elsa.Secrets.Options;
using Elsa.Secrets.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Secrets
{
    public class SecretsOptionsBuilder
    {
        public SecretsOptionsBuilder(IServiceCollection services)
        {
            SectersOptions = new SectersOptions();
            Services = services;
            services.TryAddSingleton<ICacheSignal, CacheSignal>();
        }

        public IServiceCollection Services { get; }
        public SectersOptions SectersOptions { get; }

        public SecretsOptionsBuilder UseWorkflowSettingsStore(Func<IServiceProvider, ISecretsStore> factory)
        {
            SectersOptions.SecretsStoreFactory = factory;
            return this;
        }
    }
}