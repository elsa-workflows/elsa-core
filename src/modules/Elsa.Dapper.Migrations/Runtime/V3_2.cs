using System.Diagnostics.CodeAnalysis;
using FluentMigrator;
using JetBrains.Annotations;
using static System.Int32;

namespace Elsa.Dapper.Migrations.Runtime;

/// <inheritdoc />
[Migration(20002, "Elsa:Runtime:V3.2")]
[PublicAPI]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class V3_2 : Migration
{
    /// <inheritdoc />
    public override void Up()
    {
        Delete.Table("WorkflowInboxMessages");
    }

    /// <inheritdoc />
    public override void Down()
    {
        IfDatabase("SqlServer", "Oracle", "MySql", "Postgres")
            .Create
            .Table("WorkflowInboxMessages")
            .WithColumn("Id").AsString().PrimaryKey()
            .WithColumn("ActivityTypeName").AsString().NotNullable().Indexed()
            .WithColumn("WorkflowInstanceId").AsString().Nullable().Indexed()
            .WithColumn("ActivityInstanceId").AsString().Nullable().Indexed()
            .WithColumn("CorrelationId").AsString().Nullable().Indexed()
            .WithColumn("Hash").AsString().NotNullable().Indexed()
            .WithColumn("SerializedBookmarkPayload").AsString(MaxValue)
            .WithColumn("SerializedInput").AsString(MaxValue).Nullable()
            .WithColumn("CreatedAt").AsDateTimeOffset().Indexed()
            .WithColumn("ExpiresAt").AsDateTimeOffset().Indexed()
            ;
        
        IfDatabase("Sqlite")
            .Create
            .Table("WorkflowInboxMessages")
            .WithColumn("Id").AsString().PrimaryKey()
            .WithColumn("ActivityTypeName").AsString().NotNullable().Indexed()
            .WithColumn("WorkflowInstanceId").AsString().Nullable().Indexed()
            .WithColumn("ActivityInstanceId").AsString().Nullable().Indexed()
            .WithColumn("CorrelationId").AsString().Nullable().Indexed()
            .WithColumn("Hash").AsString().NotNullable().Indexed()
            .WithColumn("SerializedBookmarkPayload").AsString(MaxValue)
            .WithColumn("SerializedInput").AsString(MaxValue).Nullable()
            .WithColumn("CreatedAt").AsDateTime2().Indexed()
            .WithColumn("ExpiresAt").AsDateTime2().Indexed()
            ;
    }
}