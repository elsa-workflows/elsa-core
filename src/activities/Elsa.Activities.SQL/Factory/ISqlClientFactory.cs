using Elsa.Activities.Sql.Client;

namespace Elsa.Activities.Sql.Factory
{
    public interface ISqlClientFactory
    {
        ISqlServerClient CreateClient(string connectionString);
    }
}
