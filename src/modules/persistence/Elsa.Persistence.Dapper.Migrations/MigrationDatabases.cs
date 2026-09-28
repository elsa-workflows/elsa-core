namespace Elsa.Persistence.Dapper.Migrations;

/// <summary>
/// Provider names passed to FluentMigrator's alias-aware <c>IfDatabase(params string[])</c> overload.
/// </summary>
/// <remarks>
/// <c>IfDatabase(params string[])</c> does an exact <see cref="StringComparison.OrdinalIgnoreCase"/>
/// match against the processor's <c>DatabaseType</c> <em>or</em> <c>DatabaseTypeAliases</c>.
/// It is not a prefix match. The predicate overload sees only <c>DatabaseType</c> and must not
/// be used here: FluentMigrator 7.2 reports <c>SqlServer2016</c>, <c>MySql8</c>,
/// <c>OracleManaged</c>, <c>PostgreSQL15_0</c>, etc. as <c>DatabaseType</c>, with
/// <c>SqlServer</c> / <c>MySql</c> / <c>Oracle</c> / <c>PostgreSQL</c> as aliases.
/// </remarks>
internal static class MigrationDatabases
{
    /// <summary>
    /// Names matched against DatabaseType or aliases for the DateTimeOffset create-table branch
    /// (everything except SQLite).
    /// </summary>
    /// <remarks>
    /// <c>Postgres</c> covers the pre-7.2 <c>PostgresProcessor</c>.
    /// <c>PostgreSQL</c> is an alias on FluentMigrator 7.2 <c>AddPostgres()</c> /
    /// <c>AddPostgres10_0()</c> / <c>AddPostgres11_0()</c> / <c>AddPostgres15_0()</c>.
    /// <c>PostgreSQL92</c> is an alias on <c>AddPostgres92()</c> (DatabaseType <c>Postgres92</c>).
    /// </remarks>
    public static readonly string[] DateTimeOffsetProviders =
    [
        "SqlServer",
        "Oracle",
        "MySql",
        "Postgres",
        "PostgreSQL",
        "PostgreSQL92"
    ];
}
