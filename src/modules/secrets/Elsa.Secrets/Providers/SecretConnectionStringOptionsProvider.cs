using System.Linq;
using System.Reflection;
using Elsa.Activities.Sql.Activities;
using Elsa.Design;
using Elsa.Metadata;
using Elsa.Secrets.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.Providers
{
    public class SecretConnectionStringOptionsProvider : IActivityPropertyOptionsProvider
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public SecretConnectionStringOptionsProvider(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

        public object? GetOptions(PropertyInfo property)
        {
            if (property.Name != nameof(ExecuteSqlQuery.ConnectionString)
                || property.Name != nameof(ExecuteSqlCommand.ConnectionString)) return null;

            using var scope = _scopeFactory.CreateScope();
            var secretsProvider = scope.ServiceProvider.GetRequiredService<ISecretsProvider>();

            var secretsPostgre = secretsProvider.GetSecretsForSelectListAsync(Constants.SecretType_PostgreSql).Result;
            var secretsMssql = secretsProvider.GetSecretsForSelectListAsync(Constants.SecretType_MsSql).Result;

            var items = secretsMssql.Select(x => new SelectListItem(x.Item1, x.Item2)).ToList();
            items.AddRange(secretsPostgre.Select(x => new SelectListItem(x.Item1, x.Item2)).ToList());
            items.Insert(0, new SelectListItem("", "empty"));

            return new SelectList { Items = items };
        }
    }
}
