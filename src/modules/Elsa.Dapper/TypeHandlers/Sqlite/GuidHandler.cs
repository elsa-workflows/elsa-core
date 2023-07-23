namespace Elsa.Dapper.TypeHandlers.Sqlite;

/// <summary>
/// Represents a SQLite type handler for <see cref="Guid"/>.
/// </summary>
internal class GuidHandler : SqliteTypeHandler<Guid>
{
    public override Guid Parse(object value) => Guid.Parse((string)value);
}