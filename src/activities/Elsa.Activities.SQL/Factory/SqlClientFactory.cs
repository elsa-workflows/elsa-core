using Elsa.Activities.Sql.Client;

namespace Elsa.Activities.Sql.Factory
{
    public class SqlClientFactory : ISqlClientFactory
    {
        public ISqlServerClient CreateClient(string connectionString)
        { 
            return new SqlServerClient(connectionString);
        }
    }
}
