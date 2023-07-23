using System.Data;
using Dapper;

namespace Elsa.Dapper.TypeHandlers.Sqlite;

/// <summary>
/// Represents a SQLite type handler.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
internal abstract class SqliteTypeHandler<T> : SqlMapper.TypeHandler<T>
{
    // Parameters are converted by Microsoft.Data.Sqlite
    public override void SetValue(IDbDataParameter parameter, T value) => parameter.Value = value;
}