using Elsa.Attributes;
using Elsa.Persistence.EntityFramework.Core;
using Elsa.Persistence.EntityFramework.Core.Options;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFramework.MySql
{
    [Feature("DefaultPersistence:EntityFrameworkCore:MySql")]
    public class Startup : EntityFrameworkCoreStartupBase
    {
        protected override string ProviderName => "MySql";
        protected override void Configure(DbContextOptionsBuilder options, ElsaDbOptions elsaDbOptions)
            => options.UseMySql(elsaDbOptions.ConnectionString);
    }
}