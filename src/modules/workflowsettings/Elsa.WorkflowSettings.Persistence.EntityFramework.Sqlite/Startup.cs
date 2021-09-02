using Elsa.Attributes;
using Elsa.WorkflowSettings.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;

namespace Elsa.WorkflowSettings.Persistence.EntityFramework.Sqlite
{
    [Feature("WorkflowSettings:EntityFrameworkCore:Sqlite")]
    public class Startup : EntityFrameworkWorkflowSettingsStartupBase
    {  
        protected override string ProviderName => "Sqlite";
        protected override string GetDefaultConnectionString() => "Data Source=elsa.sqlite.db;Cache=Shared;";
        protected override void Configure(DbContextOptionsBuilder options, string connectionString) => options.UseWorkflowSettingsSqlite(connectionString);
    }
}