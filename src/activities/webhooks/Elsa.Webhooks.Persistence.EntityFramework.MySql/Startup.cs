using System;
using Elsa.Abstractions.MultiTenancy;
using Elsa.Attributes;
using Elsa.Webhooks.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Webhooks.Persistence.EntityFramework.MySql
{
    [Feature("Webhooks:EntityFrameworkCore:MySql")]
    public class Startup : EntityFrameworkWebhookStartupBase
    {
        protected override string ProviderName => "MySql";
        protected override void Configure(DbContextOptionsBuilder options, string connectionString) => options.UseWebhookMySql(connectionString);
        protected override void ConfigureFor(DbContextOptionsBuilder options, IServiceProvider serviceProvider)
        {
            var tenantProvider = serviceProvider.GetRequiredService<ITenantProvider>();

            var connectionString = tenantProvider.GetCurrentTenant().ConnectionString;

            options.UseWebhookMySql(connectionString);
        }
    }
}