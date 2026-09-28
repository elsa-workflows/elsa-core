namespace Elsa.Persistence.Dapper.Migrations;

/// <summary>
/// Provider matching for Dapper <c>IfDatabase</c> create-table branches.
/// </summary>
/// <remarks>
/// <para>
/// <c>IfDatabase(params string[])</c> is an exact <see cref="StringComparison.OrdinalIgnoreCase"/>
/// match against the processor's <c>DatabaseType</c> <em>and</em> <c>DatabaseTypeAliases</c>.
/// It is not a prefix match.
/// </para>
/// <para>
/// <c>IfDatabase(Predicate&lt;string&gt;)</c> receives only <c>DatabaseType</c> (aliases are ignored).
/// FluentMigrator 7.2 postgres processors all report a <c>DatabaseType</c> that starts with
/// <c>Postgres</c>: <c>Postgres</c>, <c>PostgreSQL</c>, <c>PostgreSQL10_0</c>, <c>PostgreSQL11_0</c>,
/// <c>PostgreSQL15_0</c>, <c>Postgres92</c>, <c>PostgreSQL92</c>. A case-insensitive prefix check
/// therefore matches every 7.2 postgres processor, including <c>AddPostgres()</c>'s
/// <c>PostgreSQL15_0</c>, without relying on aliases.
/// </para>
/// </remarks>
internal static class MigrationDatabases
{
    public static bool IsDateTimeOffsetProvider(string databaseType) =>
        databaseType.Equals("SqlServer", StringComparison.OrdinalIgnoreCase)
        || databaseType.Equals("Oracle", StringComparison.OrdinalIgnoreCase)
        || databaseType.Equals("MySql", StringComparison.OrdinalIgnoreCase)
        || databaseType.StartsWith("Postgres", StringComparison.OrdinalIgnoreCase);
}
