using Elsa.Attributes;
using Elsa.Persistence.EntityFramework.Core.Options;
using Elsa.WorkflowSettings.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;

namespace Elsa.WorkflowSettings.Persistence.EntityFramework.MySql
{
    [Feature("WorkflowSettings:EntityFrameworkCore:MySql")]
    public class Startup : EntityFrameworkWorkflowSettingsStartupBase
    {
        protected override string ProviderName => "MySql";
        protected override void Configure(DbContextOptionsBuilder options, ElsaDbOptions elsaDbOptions)
            => options.UseWorkflowSettingsMySql(elsaDbOptions.ConnectionString);
    }
}