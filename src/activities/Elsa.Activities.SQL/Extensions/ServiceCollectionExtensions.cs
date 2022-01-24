
using Elsa.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Activities.SQL.Extensions
{
    using PostgreSql = Elsa.Activities.SQL.Activities.PostgreSql;
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder AddSqlActivities(this ElsaOptionsBuilder elsa)
        {
            return elsa.AddActivity<PostgreSql>();
        }
    }
}
