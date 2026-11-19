using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace Elsa.Persistence.Dapper.Services;

internal static class DapperExceptionClassifier
{
    public static bool IsDuplicateKey(DbException exception) => exception switch
    {
        SqliteException { SqliteExtendedErrorCode: 1555 or 2067 } => true,
        PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } => true,
        SqlException { Number: 2601 or 2627 } => true,
        _ => false
    };
}
