using Elsa.Connections.Credentials.Persistence.EFCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Connections.Credentials.Persistence.EFCore.Sqlite;

public sealed class SqliteConnectionCredentialBindingConflictClassifier : IConnectionCredentialBindingConflictClassifier
{
    private const int SqliteConstraintPrimaryKey = 1555;
    private const int SqliteConstraintUnique = 2067;

    public bool IsDuplicateBindingKey(DbUpdateException exception) =>
        EnumerateExceptions(exception)
            .OfType<SqliteException>()
            .Any(sqliteException => sqliteException.SqliteExtendedErrorCode is SqliteConstraintPrimaryKey or SqliteConstraintUnique);

    private static IEnumerable<Exception> EnumerateExceptions(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            yield return current;
        }
    }
}
