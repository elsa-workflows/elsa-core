using Elsa.Secrets.Persistence;
using Elsa.Secrets.Persistence.InMemory;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Elsa.Secrets.Options
{
    public class SecretsOptions
    {
        public SecretsOptions()
        {
            SecretsStoreFactory = provider => ActivatorUtilities.CreateInstance<InMemorySecretsStore>(provider);
        }

        public Func<IServiceProvider, ISecretsStore> SecretsStoreFactory { get; set; }
    }
}
