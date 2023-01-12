using Elsa.Expressions;
using Elsa.Options;
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
                .AddScoped<ISecretValueFormatter, MsSqlSecretValueFormatter>()
                .AddScoped<ISecretValueFormatter, PostgreSqlSecretValueFormatter>()
                .AddScoped<ISecretValueFormatter, AuthorizationHeaderSecretValueFormatter>()
                .AddScoped<ISecretsManager, SecretsManager>()
                .AddScoped<ISecretsProvider, SecretsProvider>()
                .Decorate<ISecretsStore, EventPublishingSecretsStore>()
                .AddNotificationHandlersFrom<DescribingActivityTypeHandler>();

            elsaOptions.Services
                .TryAddProvider<IExpressionHandler, SecretsExpressionHandler>(ServiceLifetime.Scoped);

            return elsaOptions;
        }
    }
}
