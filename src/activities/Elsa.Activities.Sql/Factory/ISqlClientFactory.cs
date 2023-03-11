using Elsa.Activities.Sql.Client;
using Elsa.Activities.Sql.Models;
using System.Collections.Generic;

namespace Elsa.Activities.Sql.Factory
{
    public interface ISqlClientFactory
    {
        List<string> Databases { get; }

        ISqlClient CreateClient(CreateSqlClientModel createSqlClient);
    }
}
