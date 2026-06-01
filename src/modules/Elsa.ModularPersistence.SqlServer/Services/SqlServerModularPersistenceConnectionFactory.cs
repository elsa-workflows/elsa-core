using System.Data.Common;
using Elsa.ModularPersistence.Relational.Contracts;
using Elsa.ModularPersistence.SqlServer.Options;
using Microsoft.Data.SqlClient;

namespace Elsa.ModularPersistence.SqlServer.Services;

/// <summary>
/// Opens SQL Server connections for modular persistence.
/// </summary>
public sealed class SqlServerModularPersistenceConnectionFactory(SqlServerModularPersistenceOptions options) : IRelationalConnectionFactory
{
    public async ValueTask<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
