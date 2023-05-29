using System.Linq;
using System.Reflection;
using Elsa.Design;
using Elsa.Metadata;
using Elsa.Secrets.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.Providers
{
    /// <summary>
    /// Options provider to list connection string secrets.
    /// </summary>
    public class SecretConnectionStringOptionsProvider : IActivityPropertyOptionsProvider
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public SecretConnectionStringOptionsProvider(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

        public object? GetOptions(PropertyInfo property)
        {
            using var scope = _scopeFactory.CreateScope();
            var secretsProvider = scope.ServiceProvider.GetRequiredService<ISecretsProvider>();

            var secretsPostgre = secretsProvider.GetSecretsDictionaryAsync(Constants.SecretType_PostgreSql).Result;
            var secretsMssql = secretsProvider.GetSecretsDictionaryAsync(Constants.SecretType_MsSql).Result;
            var secretsMysql = secretsProvider.GetSecretsDictionaryAsync(Constants.SecretType_MySql).Result;

            var items = secretsMssql.Select(x => new SelectListItem(x.Value, x.Key)).ToList();
            items.AddRange(secretsPostgre.Select(x => new SelectListItem(x.Value, x.Key)).ToList());
            items.AddRange(secretsMysql.Select(x => new SelectListItem(x.Value, x.Key)).ToList());
            items.Insert(0, new SelectListItem("", "empty"));

            return new SelectList { Items = items };
        }
    }
}
