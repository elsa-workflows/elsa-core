namespace Elsa.Persistence.EFCore;

internal static class DbExceptionClassifier
{
    private static readonly HashSet<int> SqlServerTransientErrorNumbers =
    [
        -2,
        64,
        233,
        1205,
        4060,
        10928,
        10929,
        40197,
        40501,
        40613,
        49918,
        49919,
        49920,
    ];

    public static bool IsSqlServerTransient(string providerName, Exception exception)
    {
        if (!providerName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            return false;

        return EnumerateExceptions(exception).Any(IsSqlServerTransientException);
    }

    public static bool IsDuplicateKey(Exception exception)
    {
        return EnumerateExceptions(exception).Any(IsDuplicateKeyException);
    }

    private static bool IsSqlServerTransientException(Exception exception)
    {
        if (!IsSqlClientException(exception))
            return false;

        return GetErrorNumbers(exception).Any(SqlServerTransientErrorNumbers.Contains)
               || exception.Message.Contains("deadlock", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDuplicateKeyException(Exception exception)
    {
        var type = exception.GetType();
        var typeName = type.Name;
        var typeNamespace = type.Namespace ?? string.Empty;
        var errorNumbers = GetErrorNumbers(exception).ToList();

        if (IsSqlClientException(exception) && errorNumbers.Any(number => number is 2601 or 2627))
            return true;

        if (typeName.Contains("MySql", StringComparison.OrdinalIgnoreCase) && errorNumbers.Contains(1062))
            return true;

        if (typeName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) && errorNumbers.Any(number => number is 19 or 1555 or 2067))
            return true;

        if (typeName.Contains("Oracle", StringComparison.OrdinalIgnoreCase) && errorNumbers.Contains(1))
            return true;

        if (GetStringProperty(exception, "SqlState") == "23505")
            return true;

        return typeNamespace.Contains("Data", StringComparison.OrdinalIgnoreCase)
               && (exception.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
               || exception.Message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase)
               || exception.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase)
               || exception.Message.Contains("ORA-00001", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsSqlClientException(Exception exception)
    {
        var type = exception.GetType();
        return type.Name.Equals("SqlException", StringComparison.OrdinalIgnoreCase)
               && type.Namespace?.Contains("SqlClient", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static IEnumerable<int> GetErrorNumbers(object source)
    {
        if (GetIntProperty(source, "Number") is { } number)
            yield return number;

        if (GetIntProperty(source, "SqliteErrorCode") is { } sqliteErrorCode)
            yield return sqliteErrorCode;

        if (GetIntProperty(source, "SqliteExtendedErrorCode") is { } sqliteExtendedErrorCode)
            yield return sqliteExtendedErrorCode;

        var errors = source.GetType().GetProperty("Errors")?.GetValue(source);
        if (errors is not System.Collections.IEnumerable errorCollection)
            yield break;

        foreach (var error in errorCollection)
        {
            if (error is null)
                continue;

            if (GetIntProperty(error, "Number") is { } errorNumber)
                yield return errorNumber;
        }
    }

    private static int? GetIntProperty(object source, string name)
    {
        var value = source.GetType().GetProperty(name)?.GetValue(source);
        return value switch
        {
            int number => number,
            short number => number,
            long number when number is >= int.MinValue and <= int.MaxValue => (int)number,
            _ => null
        };
    }

    private static string? GetStringProperty(object source, string name)
    {
        return source.GetType().GetProperty(name)?.GetValue(source) as string;
    }

    private static IEnumerable<Exception> EnumerateExceptions(Exception exception)
    {
        var stack = new Stack<Exception>();
        stack.Push(exception);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            yield return current;

            if (current is AggregateException aggregateException)
            {
                foreach (var inner in aggregateException.InnerExceptions)
                    stack.Push(inner);
            }
            else if (current.InnerException is not null)
            {
                stack.Push(current.InnerException);
            }
        }
    }
}
