using Elsa.Options;
using Elsa.Secrets.Manager;
using Elsa.Secrets.Persistence;
using Elsa.Secrets.Persistence.Decorators;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Secrets.Extentions
{
    public static class SecretsOptionsBuilderExtensions
    {
        public static ElsaOptionsBuilder AddSecrets(this ElsaOptionsBuilder elsaOptions)
        {
            elsaOptions.Services
                .AddScoped<ISecretsManager, SecretsManager>()
                .Decorate<ISecretsStore, InitializingSecretsStore>()
                .Decorate<ISecretsStore, EventPublishingSecretsStore>();

            return elsaOptions;
        }
    }
}
