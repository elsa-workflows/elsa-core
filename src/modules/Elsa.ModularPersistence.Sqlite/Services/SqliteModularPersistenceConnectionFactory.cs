using System.Data.Common;
using Elsa.ModularPersistence.Relational.Contracts;
using Elsa.ModularPersistence.Sqlite.Options;
using Microsoft.Data.Sqlite;

namespace Elsa.ModularPersistence.Sqlite.Services;

/// <summary>
/// Opens SQLite connections for modular persistence.
/// </summary>
public sealed class SqliteModularPersistenceConnectionFactory(SqliteModularPersistenceOptions options) : IRelationalConnectionFactory
{
    public async ValueTask<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqliteConnection(options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
