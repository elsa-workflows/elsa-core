using System;
using System.Threading.Tasks;
using Elsa.Abstractions.Multitenancy;
using Elsa.Attributes;
using Elsa.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EntityFramework.SqlServer
{
    [Feature("DefaultPersistence:EntityFrameworkCore:SqlServer")]
    public class Startup : EntityFrameworkCoreStartupBase
    {
        protected override string ProviderName => "SqlServer";
        protected override void Configure(DbContextOptionsBuilder options, string connectionString) => options.UseSqlServer(connectionString);
        protected override async Task Configure(DbContextOptionsBuilder options, IServiceProvider serviceProvider)
        {
            var tenantProvider = serviceProvider.GetRequiredService<ITenantProvider>();
            var tenant = await tenantProvider.GetCurrentTenantAsync();

            var connectionString = tenant.GetDatabaseConnectionString();

            options.UseSqlServer(connectionString);
        }
    }
}