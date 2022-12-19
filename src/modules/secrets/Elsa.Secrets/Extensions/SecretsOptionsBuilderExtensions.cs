using Elsa.Expressions;
using Elsa.Options;
using Elsa.Secrets.Enrichers;
using Elsa.Secrets.Handlers;
using Elsa.Secrets.Manager;
using Elsa.Secrets.Persistence;
using Elsa.Secrets.Persistence.Decorators;
using Elsa.Secrets.Providers;
using Elsa.Secrets.ValueFormatters;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.Extensions
{
    public static class SecretsOptionsBuilderExtensions
    {
        public static ElsaOptionsBuilder AddSecrets(this ElsaOptionsBuilder elsaOptions)
        {
            elsaOptions.Services
                .AddSingleton<ISecretValueFormatter, MsSqlSecretValueFormatter>()
                .AddSingleton<ISecretValueFormatter, PostgreSqlSecretValueFormatter>()
                .AddSingleton<ISecretValueFormatter, AuthorizationHeaderSecretValueFormatter>()
                .AddSingleton<ISecretValueFormatter, OAuth2SecretValueFormatter>()
                .AddScoped<ISecretsManager, SecretsManager>()
                .AddScoped<ISecretsProvider, SecretsProvider>()
                .Decorate<ISecretsStore, EventPublishingSecretsStore>()
                .AddNotificationHandlersFrom<DescribingActivityTypeHandler>();

            elsaOptions.Services
                .TryAddProvider<IExpressionHandler, SecretsHandler>(ServiceLifetime.Scoped);

            return elsaOptions;
        }
    }
}
