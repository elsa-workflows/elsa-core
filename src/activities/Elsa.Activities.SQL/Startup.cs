using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Activities.SQL.Extensions;
using Elsa.Activities.SQL.Persistence;
using Elsa.Activities.SQL.Services;
using Elsa.Attributes;
using Elsa.Options;
using Elsa.Persistence.EntityFramework.Core.Services;
using Elsa.Services.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.SQL
{
    [Feature("SQL")]
    public class Startup : StartupBase
    {
        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            elsa.AddSqlActivities();

            elsa.Services.AddScoped<ElsaContextFactory<SqlActivityContext>>();
        }

    }
}
