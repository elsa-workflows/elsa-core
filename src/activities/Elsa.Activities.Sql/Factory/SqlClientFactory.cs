using Elsa.Activities.Sql.Client;
using Elsa.Activities.Sql.Client.PostgreSqlClient;
using Elsa.Activities.Sql.Models;
using System;
using System.Collections.Generic;

namespace Elsa.Activities.Sql.Factory
{
    public class SqlClientFactory : ISqlClientFactory
    {
        public List<string> Databases => new List<string>() { "MSSQLServer", "MSSQL Server", "PostgreSql" };

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
