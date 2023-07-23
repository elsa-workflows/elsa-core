namespace Elsa.Dapper.TypeHandlers.Sqlite;

/// <summary>
/// Represents a SQLite type handler for <see cref="TimeSpan"/>.
/// </summary>
internal class TimeSpanHandler : SqliteTypeHandler<TimeSpan>
{
    public override TimeSpan Parse(object value) => TimeSpan.Parse((string)value);
}