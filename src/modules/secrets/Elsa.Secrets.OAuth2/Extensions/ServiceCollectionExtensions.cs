using Elsa.Options;
using Elsa.Secrets.OAuth2.ValueFormatters;
using Elsa.Secrets.ValueFormatters;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.OAuth2.Extensions {
    public static class ServiceCollectionExtensions {
        public static ElsaOptionsBuilder AddOauth2Services(this ElsaOptionsBuilder options)
        {
            options.Services
                .AddSingleton<ISecretValueFormatter, OAuth2SecretValueFormatter>();

            return options;
        }
    }
}