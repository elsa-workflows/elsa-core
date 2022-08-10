using Elsa.Attributes;
using Elsa.Persistence.EntityFramework.Core.Options;
using Elsa.WorkflowSettings.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;

namespace Elsa.WorkflowSettings.Persistence.EntityFramework.PostgreSql
{
    [Feature("WorkflowSettings:EntityFrameworkCore:PostgreSql")]
    public class Startup : EntityFrameworkWorkflowSettingsStartupBase
    {
        protected override string ProviderName => "PostgreSql";
        protected override void Configure(DbContextOptionsBuilder options, ElsaDbOptions elsaDbOptions)
            => options.UseWorkflowSettingsPostgreSql(elsaDbOptions.ConnectionString);
    }
}