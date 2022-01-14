using System;
using Elsa.Abstractions.MultiTenancy;
using Elsa.Attributes;
using Elsa.Webhooks.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Webhooks.Persistence.EntityFramework.Sqlite
{
    [Feature("Webhooks:EntityFrameworkCore:Sqlite")]
    public class Startup : EntityFrameworkWebhookStartupBase
    {
        protected override string ProviderName => "Sqlite";
        protected override string GetDefaultConnectionString() => "Data Source=elsa.sqlite.db;Cache=Shared;";
        protected override void Configure(DbContextOptionsBuilder options, string connectionString) => options.UseWebhookSqlite(connectionString);
        protected override void ConfigureForMultitenancy(DbContextOptionsBuilder options, IServiceProvider serviceProvider)
        {
            var tenantProvider = serviceProvider.GetRequiredService<ITenantProvider>();

            var connectionString = tenantProvider.GetCurrentTenant().ConnectionString;

            options.UseWebhookSqlite(connectionString);
        }
    }
}