using System.Data;
using Dapper;

namespace Elsa.Persistence.Dapper.TypeHandlers.PostgreSql;

/// <summary>
/// Maps PostgreSQL timestamptz values to <see cref="DateTimeOffset"/>.
/// </summary>
/// <remarks>
/// Npgsql returns <see cref="DateTime"/>. The SQLite handler registered by
/// <c>SqliteDbConnectionProvider</c> assumes a string and throws
/// <see cref="InvalidCastException"/> on that DateTime. This handler accepts
/// DateTime, DateTimeOffset, and string so PG reads work even when the SQLite
/// handler was registered earlier in the process.
/// </remarks>
internal sealed class DateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset>
{
    public override void SetValue(IDbDataParameter parameter, DateTimeOffset value) => parameter.Value = value;

    public override DateTimeOffset Parse(object value) => value switch
    {
        DateTimeOffset dto => dto,
        DateTime dt => dt.Kind == DateTimeKind.Unspecified
            ? new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc))
            : new DateTimeOffset(dt),
        string s => DateTimeOffset.Parse(s),
        _ => throw new InvalidCastException($"Cannot convert {value.GetType()} to DateTimeOffset.")
    };
}
