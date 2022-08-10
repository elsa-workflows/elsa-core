using Elsa.Attributes;
using Elsa.Persistence.EntityFramework.Core.Options;
using Elsa.WorkflowSettings.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;

namespace Elsa.WorkflowSettings.Persistence.EntityFramework.SqlServer
{
    [Feature("WorkflowSettings:EntityFrameworkCore:SqlServer")]
    public class Startup : EntityFrameworkWorkflowSettingsStartupBase
    {
        protected override string ProviderName => "SqlServer";
        protected override void Configure(DbContextOptionsBuilder options, ElsaDbOptions elsaDbOptions)
            => options.UseWorkflowSettingsSqlServer(elsaDbOptions.ConnectionString);
    }
}