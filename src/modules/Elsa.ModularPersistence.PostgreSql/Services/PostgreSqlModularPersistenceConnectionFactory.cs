using System.Data.Common;
using Elsa.ModularPersistence.Relational.Contracts;
using Elsa.ModularPersistence.PostgreSql.Options;
using Npgsql;

namespace Elsa.ModularPersistence.PostgreSql.Services;

/// <summary>
/// Opens PostgreSQL connections for modular persistence.
/// </summary>
public sealed class PostgreSqlModularPersistenceConnectionFactory(PostgreSqlModularPersistenceOptions options) : IRelationalConnectionFactory
{
    public async ValueTask<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
