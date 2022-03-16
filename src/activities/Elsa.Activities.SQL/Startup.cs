using Elsa.Activities.Sql.Extensions;
using Elsa.Attributes;
using Elsa.Options;
using Elsa.Services.Startup;
using Microsoft.Extensions.Configuration;

namespace Elsa.Activities.Sql
{
    [Feature("ExecuteSqlServerQuery")]
    public class Startup : StartupBase
    {
        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            elsa.AddSqlServerActivities();
        }

    }
}
