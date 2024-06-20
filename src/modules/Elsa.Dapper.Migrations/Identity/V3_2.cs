using System.Diagnostics.CodeAnalysis;
using FluentMigrator;
using JetBrains.Annotations;

namespace Elsa.Dapper.Migrations.Identity;

/// <inheritdoc />
[Migration(30003, "Elsa:Identity:V3.2")]
[PublicAPI]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class V3_2 : Migration
{
    /// <inheritdoc />
    public override void Up()
    {
        Alter.Table("Users").AddColumn("TenantId").AsString().Nullable();
        Alter.Table("Roles").AddColumn("TenantId").AsString().Nullable();
        Alter.Table("Applications").AddColumn("TenantId").AsString().Nullable();
    }

    /// <inheritdoc />
    public override void Down()
    {
        Delete.Column("TenantId").FromTable("Users");
        Delete.Column("TenantId").FromTable("Roles");
        Delete.Column("TenantId").FromTable("Applications");
    }
}