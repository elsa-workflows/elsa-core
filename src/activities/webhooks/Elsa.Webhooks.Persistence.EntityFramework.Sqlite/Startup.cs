using Elsa.Attributes;
using Elsa.Webhooks.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Webhooks.Persistence.EntityFramework.Sqlite
{
    [Feature("Webhooks:EntityFrameworkCore:Sqlite")]
    public class Startup : EntityFrameworkWebhookStartupBase
    {
        protected override string ProviderName => "Sqlite";
        protected override string GetDefaultConnectionString() => "Data Source=elsa.sqlite.db;Cache=Shared;";
        protected override void Configure(DbContextOptionsBuilder options, string connectionString) => options.UseWebhookSqlite(connectionString);
    }
}