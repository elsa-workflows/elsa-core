using System.Diagnostics.CodeAnalysis;
using FluentMigrator;
using JetBrains.Annotations;

namespace Elsa.Dapper.Migrations.Runtime;

/// <inheritdoc />
[Migration(20003, "Elsa:Runtime:V3.2")]
[PublicAPI]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class V3_2 : Migration
{
    /// <inheritdoc />
    public override void Up()
    {
        Alter.Table("Triggers").AddColumn("TenantId").AsString().Nullable();
        Alter.Table("Bookmarks").AddColumn("TenantId").AsString().Nullable();
        Alter.Table("WorkflowExecutionLogRecords").AddColumn("TenantId").AsString().Nullable();
        Alter.Table("ActivityExecutionRecords").AddColumn("TenantId").AsString().Nullable();
        Alter.Table("WorkflowInboxMessages").AddColumn("TenantId").AsString().Nullable();
        Alter.Table("KeyValuePairs").AddColumn("TenantId").AsString().Nullable();
    }

    /// <inheritdoc />
    public override void Down()
    {
        Delete.Column("TenantId").FromTable("Triggers");
        Delete.Column("TenantId").FromTable("Bookmarks");
        Delete.Column("TenantId").FromTable("WorkflowExecutionLogRecords");
        Delete.Column("TenantId").FromTable("ActivityExecutionRecords");
        Delete.Column("TenantId").FromTable("WorkflowInboxMessages");
        Delete.Column("TenantId").FromTable("KeyValuePairs");
    }
}