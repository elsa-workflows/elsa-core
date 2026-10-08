using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using FluentMigrator;
using JetBrains.Annotations;

namespace Elsa.Persistence.Dapper.Migrations.Identity;

/// <summary>
/// Adds a unique index on <c>Roles (TenantId, Name)</c>, so two tenants can each hold a role with the same name while
/// one tenant cannot hold two (elsa-core#8615, elsa-extensions#282).
/// </summary>
/// <remarks>
/// <para>
/// Earlier versions had no role name uniqueness in the Dapper schema, so existing data can already contain duplicates.
/// This migration never rewrites or deletes rows. Before it creates the index it checks for same-tenant duplicate names,
/// including names that differ only in case, which <c>RoleManager</c> treats as the same role. If it finds any, it fails
/// with the colliding IDs and changes nothing; the operator resolves those rows and runs the migration again.
/// </para>
/// <para>
/// For that check, a <c>NULL</c> and a <c>''</c> tenant ID both count as the default tenant: <c>NULL</c> is what
/// single-tenant installs and rows written before multitenancy store, and <c>''</c> is the default tenant's ID.
/// The index itself is on the stored values. SQL Server treats NULLs as equal in a unique index, so it allows one
/// <c>(NULL, Name)</c> row per name. SQLite, PostgreSQL, MySQL and Oracle treat NULLs as distinct, so the index does not
/// reject two <c>NULL</c>-tenant rows, or a <c>NULL</c>/<c>''</c> pair, with the same name; there, name uniqueness for
/// rows without a tenant stays with the application's pre-save check.
/// </para>
/// <para>
/// The index follows the database collation: typically case-insensitive on SQL Server and MySQL, case-sensitive on
/// SQLite, PostgreSQL and Oracle. On the case-sensitive databases it only rejects exact duplicates, and names that differ
/// only in case are rejected by core's case-insensitive check in <c>RoleManager</c> before saving.
/// </para>
/// </remarks>
[Migration(30005, "Elsa:Identity:V3.10")]
[PublicAPI]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class V3_10 : Migration
{
    /// <summary>
    /// The name of the unique index on <c>Roles (TenantId, Name)</c>.
    /// </summary>
    public const string TenantIdNameUniqueIndex = "IX_Roles_TenantId_Name";

    /// <inheritdoc />
    public override void Up()
    {
        if (!Schema.Table("Roles").Exists() || !Schema.Table("Roles").Column("TenantId").Exists())
        {
            return;
        }

        if (Schema.Table("Roles").Index(TenantIdNameUniqueIndex).Exists())
        {
            return;
        }

        // The duplicate pre-check only runs on the providers listed in QuotedIdentifiers and UnquotedIdentifiers. Any
        // other provider skips it silently and goes straight to creating the index, so existing duplicates there fail
        // with the database's own unique-index error instead of the list of colliding role IDs.
        IfDatabase(MigrationDatabases.QuotedIdentifiers)
            .Execute.WithConnection((connection, transaction) => ThrowIfDuplicateTenantRoleNames(connection, transaction, quoted: true));
        IfDatabase(MigrationDatabases.UnquotedIdentifiers)
            .Execute.WithConnection((connection, transaction) => ThrowIfDuplicateTenantRoleNames(connection, transaction, quoted: false));

        Create.Index(TenantIdNameUniqueIndex)
            .OnTable("Roles")
            .OnColumn("TenantId").Ascending()
            .OnColumn("Name").Ascending()
            .WithOptions().Unique();
    }

    /// <inheritdoc />
    public override void Down()
    {
        if (Schema.Table("Roles").Index(TenantIdNameUniqueIndex).Exists())
        {
            Delete.Index(TenantIdNameUniqueIndex).OnTable("Roles");
        }
    }

    internal static void ThrowIfDuplicateTenantRoleNames(IDbConnection connection, IDbTransaction? transaction, bool quoted = false)
    {
        var duplicates = ReadRoles(connection, transaction, quoted)
            .GroupBy(role => (TenantKey: NormalizeTenantKey(role.TenantId), NameKey: role.Name.ToLowerInvariant()))
            .Where(group => group.Count() > 1)
            .OrderBy(group => group.Key.TenantKey, StringComparer.Ordinal)
            .ThenBy(group => group.Key.NameKey, StringComparer.Ordinal)
            .ToList();

        if (duplicates.Count == 0)
        {
            return;
        }

        var message = new StringBuilder()
            .Append("Cannot create unique index ")
            .Append(TenantIdNameUniqueIndex)
            .Append(" on Roles (TenantId, Name): the table already has same-tenant duplicate role names (compared case-insensitively). ")
            .Append("No rows were changed. Rename or delete the extra rows, update the users and applications that reference them, and run the migration again.");

        foreach (var group in duplicates)
        {
            message.AppendLine()
                .Append("  tenant ")
                .Append(FormatTenant(group.Key.TenantKey))
                .Append(", name(s) ")
                .Append(string.Join(" / ", group.Select(role => $"'{role.Name}'").Distinct(StringComparer.Ordinal)))
                .Append(": ")
                .Append(string.Join(", ", group.OrderBy(role => role.Id, StringComparer.Ordinal).Select(role => $"Id={role.Id}")));
        }

        throw new InvalidOperationException(message.ToString());
    }

    private static List<RoleNameRow> ReadRoles(IDbConnection connection, IDbTransaction? transaction, bool quoted)
    {
        var rows = new List<RoleNameRow>();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = quoted
            ? "SELECT \"Id\", \"TenantId\", \"Name\" FROM \"Roles\""
            : "SELECT Id, TenantId, Name FROM Roles";
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            rows.Add(new(
                reader.GetString(0),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                reader.IsDBNull(2) ? string.Empty : reader.GetString(2)));
        }

        return rows;
    }

    private static string NormalizeTenantKey(string? tenantId) => string.IsNullOrEmpty(tenantId) ? string.Empty : tenantId;

    private static string FormatTenant(string tenantKey) => tenantKey.Length == 0 ? "(default)" : $"'{tenantKey}'";

    private sealed record RoleNameRow(string Id, string? TenantId, string Name);
}
