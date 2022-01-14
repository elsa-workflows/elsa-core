using System;
using Elsa.Abstractions.MultiTenancy;
using Elsa.Attributes;
using Elsa.WorkflowSettings.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.WorkflowSettings.Persistence.EntityFramework.MySql
{
    [Feature("WorkflowSettings:EntityFrameworkCore:MySql")]
    public class Startup : EntityFrameworkWorkflowSettingsStartupBase
    {
        protected override string ProviderName => "MySql";
        protected override void Configure(DbContextOptionsBuilder options, string connectionString) => options.UseWorkflowSettingsMySql(connectionString);
        protected override void ConfigureForMultitenancy(DbContextOptionsBuilder options, IServiceProvider serviceProvider)
        {
            var tenantProvider = serviceProvider.GetRequiredService<ITenantProvider>();

            var connectionString = tenantProvider.GetCurrentTenant().ConnectionString;

            options.UseWorkflowSettingsMySql(connectionString);
        }
    }
}