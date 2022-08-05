using Elsa.Attributes;
using Elsa.Secrets.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Secrets.Persistence.EntityFramework.Sqlite
{
    [Feature("Secrets:EntityFrameworkCore:Sqlite")]
    public class Startup : EntityFrameworkSecretsStartupBase
    {
        protected override string ProviderName => "Sqlite";
        protected override string GetDefaultConnectionString() => "Data Source=elsa.sqlite.db;Cache=Shared;";
        protected override void Configure(DbContextOptionsBuilder options, string connectionString) => options.UseSecretsSqlite(connectionString);
    }
}
