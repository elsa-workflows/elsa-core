using Elsa.Attributes;
using Elsa.Persistence.EntityFramework.Core.Options;
using Elsa.Webhooks.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Webhooks.Persistence.EntityFramework.Sqlite
{
    [Feature("Webhooks:EntityFrameworkCore:Sqlite")]
    public class Startup : EntityFrameworkWebhookStartupBase
    {
        protected override string ProviderName => "Sqlite";
        protected override string GetDefaultConnectionString() => "Data Source=elsa.sqlite.db;Cache=Shared;";
        protected override void Configure(DbContextOptionsBuilder options, ElsaDbOptions elsaDbOptions)
            => options.UseWebhookSqlite(elsaDbOptions.ConnectionString);
    }
}