using FluentMigrator;
using JetBrains.Annotations;

namespace Elsa.Dapper.Migrations.Runtime;

/// <inheritdoc />
[Migration(20001, "Elsa:Runtime:Initial")]
[PublicAPI]
public class Initial : Migration
{
    /// <inheritdoc />
    public override void Up()
    {
        Create
            .Table("Triggers")
            .WithColumn("Id").AsString().PrimaryKey()
            .WithColumn("WorkflowDefinitionId").AsString().NotNullable()
            .WithColumn("Name").AsString().NotNullable()
            .WithColumn("ActivityId").AsString().NotNullable()
            .WithColumn("Hash").AsString().Nullable()
            .WithColumn("SerializedPayload").AsString().Nullable();

        Create
            .Table("Bookmarks")
            .WithColumn("Id").AsString().PrimaryKey()
            .WithColumn("ActivityTypeName").AsString().NotNullable()
            .WithColumn("WorkflowInstanceId").AsString().NotNullable()
            .WithColumn("CorrelationId").AsString().Nullable()
            .WithColumn("ActivityId").AsString().NotNullable()
            .WithColumn("Hash").AsString().Nullable()
            .WithColumn("SerializedPayload").AsString().Nullable();

        IfDatabase("SqlServer", "Oracle", "MySql", "Postgres")
            .Create
            .Table("WorkflowExecutionLogRecords")
            .WithColumn("Id").AsString().PrimaryKey()
            .WithColumn("Sequence").AsInt32().NotNullable()
            .WithColumn("WorkflowDefinitionId").AsString().NotNullable()
            .WithColumn("WorkflowInstanceId").AsString().NotNullable()
            .WithColumn("WorkflowVersion").AsInt32().NotNullable()
            .WithColumn("ActivityInstanceId").AsString().NotNullable()
            .WithColumn("ParentActivityInstanceId").AsString().Nullable()
            .WithColumn("ActivityId").AsString().NotNullable()
            .WithColumn("ActivityType").AsString().NotNullable()
            .WithColumn("NodeId").AsString().NotNullable()
            .WithColumn("EventName").AsString().Nullable()
            .WithColumn("Message").AsString().Nullable()
            .WithColumn("Source").AsString().Nullable()
            .WithColumn("SerializedActivityState").AsString().Nullable()
            .WithColumn("SerializedPayload").AsString().Nullable()
            .WithColumn("Timestamp").AsDateTimeOffset().NotNullable();

        IfDatabase("Sqlite")
            .Create
            .Table("WorkflowExecutionLogRecords")
            .WithColumn("Id").AsString().PrimaryKey()
            .WithColumn("WorkflowDefinitionId").AsString().NotNullable()
            .WithColumn("WorkflowInstanceId").AsString().NotNullable()
            .WithColumn("WorkflowVersion").AsInt32().NotNullable()
            .WithColumn("ActivityInstanceId").AsString().NotNullable()
            .WithColumn("ParentActivityInstanceId").AsString().Nullable()
            .WithColumn("ActivityId").AsString().NotNullable()
            .WithColumn("ActivityType").AsString().NotNullable()
            .WithColumn("NodeId").AsString().NotNullable()
            .WithColumn("EventName").AsString().Nullable()
            .WithColumn("Message").AsString().Nullable()
            .WithColumn("Source").AsString().Nullable()
            .WithColumn("SerializedActivityState").AsString().Nullable()
            .WithColumn("SerializedPayload").AsString().Nullable()
            .WithColumn("Timestamp").AsDateTime2().NotNullable()
            .WithColumn("Sequence").AsInt64().NotNullable();

        Create
            .Table("WorkflowStates")
            .WithColumn("Id").AsString().PrimaryKey()
            .WithColumn("DefinitionId").AsString().NotNullable()
            .WithColumn("DefinitionVersion").AsInt32().NotNullable()
            .WithColumn("CorrelationId").AsString().Nullable()
            .WithColumn("Status").AsString().NotNullable()
            .WithColumn("SubStatus").AsString().NotNullable()
            .WithColumn("ExecutionLogSequence").AsInt64().NotNullable()
            .WithColumn("Props").AsString().NotNullable()
            ;
    }

    /// <inheritdoc />
    public override void Down()
    {
        Delete.Table("Triggers");
        Delete.Table("Bookmarks");
        Delete.Table("WorkflowExecutionLogRecords");
        Delete.Table("WorkflowStates");
    }
}