using Elsa.Activities.Sql.Client;
using Elsa.Activities.Sql.Models;

namespace Elsa.Activities.Sql.Factory
{
    public interface ISqlClientFactory
    {
        ISqlClient CreateClient(CreateSqlClientModel createSqlClient);
    }
}
