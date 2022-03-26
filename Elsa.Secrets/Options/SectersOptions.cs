using Elsa.Secrets.Persistence;
using Elsa.Secrets.Persistence.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.Options
{
    public class SectersOptions
    {
        public SectersOptions()
        {
            SecretsStoreFactory = provider => ActivatorUtilities.CreateInstance<InMemorySecretsStore>(provider);
        }

        public Func<IServiceProvider, ISecretsStore> SecretsStoreFactory { get; set; }
    }
}
