using System;
using Elsa.Abstractions.MultiTenancy;
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
        protected override void ConfigureForMultitenancy(DbContextOptionsBuilder options, IServiceProvider serviceProvider)
        {
            var tenantProvider = serviceProvider.GetRequiredService<ITenantProvider>();

            var connectionString = tenantProvider.GetCurrentTenant().ConnectionString;

            options.UseSqlServer(connectionString);
        }
    }
}