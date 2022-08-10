using Elsa.Attributes;
using Elsa.Persistence.EntityFramework.Core;
using Elsa.Persistence.EntityFramework.Core.Options;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFramework.Sqlite
{
    [Feature("DefaultPersistence:EntityFrameworkCore:Sqlite")]
    public class Startup : EntityFrameworkCoreStartupBase
    {
        protected override string ProviderName => "Sqlite";
        protected override string GetDefaultConnectionString() => "Data Source=elsa.sqlite.db;Cache=Shared;";
        protected override void Configure(DbContextOptionsBuilder options, ElsaDbOptions elsaDbOptions)
            => options.UseSqlite(elsaDbOptions.ConnectionString);
    }
}