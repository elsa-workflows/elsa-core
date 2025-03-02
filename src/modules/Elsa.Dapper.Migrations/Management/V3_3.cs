using System.Diagnostics.CodeAnalysis;
using FluentMigrator;
using JetBrains.Annotations;

namespace Elsa.Dapper.Migrations.Management;

/// <inheritdoc />
[Migration(10004, "Elsa:Management:V3.3")]
[PublicAPI]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class V3_3: Migration
{
    /// <inheritdoc />
    public override void Up()
    {
        Alter.Table("WorkflowDefinitions").AddColumn("TenantId").AsString().Nullable();
        Alter.Table("WorkflowInstances").AddColumn("TenantId").AsString().Nullable();
    }

    /// <inheritdoc />
    public override void Down()
    {
        Delete.Column("TenantId").FromTable("WorkflowDefinitions");
        Delete.Column("TenantId").FromTable("WorkflowInstances");
    }
}