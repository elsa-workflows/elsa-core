using Elsa.Activities.Sql.Client;
using Elsa.Activities.Sql.Client.PostgreSqlClient;
using Elsa.Activities.Sql.Models;
using System;

namespace Elsa.Activities.Sql.Factory
{
    public class SqlClientFactory : ISqlClientFactory
    {
        public ISqlClient CreateClient(CreateSqlClientModel createSqlClient)
        {
            return createSqlClient.Database switch
            {
                "MSSQLServer" => new SqlServerClient(createSqlClient.ConnectionString),
                "MSSQL Server" => new SqlServerClient(createSqlClient.ConnectionString),
                "PostgreSql" => new PostgreSqlClient(createSqlClient.ConnectionString),
                _ => throw new ArgumentException($"Unsupported database type: {createSqlClient.Database}")
            };
        }
    }
}
