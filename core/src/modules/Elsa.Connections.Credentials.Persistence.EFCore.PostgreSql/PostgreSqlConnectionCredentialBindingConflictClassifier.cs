using Elsa.Connections.Credentials.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Elsa.Connections.Credentials.Persistence.EFCore.PostgreSql;

public sealed class PostgreSqlConnectionCredentialBindingConflictClassifier : IConnectionCredentialBindingConflictClassifier
{
    public bool IsDuplicateBindingKey(DbUpdateException exception) =>
        EnumerateExceptions(exception)
            .OfType<PostgresException>()
            .Any(postgresException => postgresException.SqlState == PostgresErrorCodes.UniqueViolation);

    public bool IsConcurrentGrantIssuanceConflict(Exception exception) =>
        EnumerateExceptions(exception)
            .OfType<PostgresException>()
            .Any(postgresException => postgresException.SqlState is PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.SerializationFailure);

    private static IEnumerable<Exception> EnumerateExceptions(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            yield return current;
        }
    }
}
