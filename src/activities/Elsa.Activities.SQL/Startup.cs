using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Activities.Sql.Extensions;
using Elsa.Attributes;
using Elsa.Options;
using Elsa.Persistence.EntityFramework.Core.Services;
using Elsa.Services.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Sql
{
    [Feature("SQL")]
    public class Startup : StartupBase
    {
        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            elsa.AddSqlActivities();
        }

    }
}
