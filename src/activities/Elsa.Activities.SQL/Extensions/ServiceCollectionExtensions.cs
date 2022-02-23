
using Elsa.Activities.Sql.Activities;
using Elsa.Options;

namespace Elsa.Activities.Sql.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder AddSqlActivities(this ElsaOptionsBuilder elsa)
        {
            return elsa.AddActivity<SqlActivity>();
        }
    }
}
