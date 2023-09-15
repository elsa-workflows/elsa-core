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
            .WithColumn("WorkflowDefinitionVersionId").AsString().NotNullable()
            .WithColumn("Name").AsString().NotNullable()
            .WithColumn("ActivityId").AsString().NotNullable()
            .WithColumn("Hash").AsString().Nullable()
            .WithColumn("SerializedPayload").AsString().Nullable();

        IfDatabase("SqlServer", "Oracle", "MySql", "Postgres")
            .Create
            .Table("Bookmarks")
            .WithColumn("Id").AsString().PrimaryKey()
            .WithColumn("ActivityTypeName").AsString().NotNullable()
            .WithColumn("WorkflowInstanceId").AsString().NotNullable()
            .WithColumn("CorrelationId").AsString().Nullable()
            .WithColumn("ActivityInstanceId").AsString().NotNullable()
            .WithColumn("Hash").AsString().Nullable()
            .WithColumn("SerializedPayload").AsString().Nullable()
            .WithColumn("SerializedMetadata").AsString().Nullable()
            .WithColumn("CreatedAt").AsDateTimeOffset().NotNullable();
        
        IfDatabase("Sqlite")
            .Create
            .Table("Bookmarks")
            .WithColumn("Id").AsString().PrimaryKey()
            .WithColumn("ActivityTypeName").AsString().NotNullable()
            .WithColumn("WorkflowInstanceId").AsString().NotNullable()
            .WithColumn("CorrelationId").AsString().Nullable()
            .WithColumn("ActivityInstanceId").AsString().NotNullable()
            .WithColumn("Hash").AsString().Nullable()
            .WithColumn("SerializedPayload").AsString().Nullable()
            .WithColumn("SerializedMetadata").AsString().Nullable()
            .WithColumn("CreatedAt").AsDateTime2().NotNullable();

        IfDatabase("SqlServer", "Oracle", "MySql", "Postgres")
            .Create
            .Table("WorkflowExecutionLogRecords")
            .WithColumn("Id").AsString().PrimaryKey()
            .WithColumn("Sequence").AsInt32().NotNullable()
            .WithColumn("WorkflowDefinitionId").AsString().NotNullable()
            .WithColumn("WorkflowDefinitionVersionId").AsString().NotNullable()
            .WithColumn("WorkflowInstanceId").AsString().NotNullable()
            .WithColumn("WorkflowVersion").AsInt32().NotNullable()
            .WithColumn("ActivityInstanceId").AsString().NotNullable()
            .WithColumn("ParentActivityInstanceId").AsString().Nullable()
            .WithColumn("ActivityId").AsString().NotNullable()
            .WithColumn("ActivityType").AsString().NotNullable()
            .WithColumn("ActivityTypeVersion").AsInt32().NotNullable()
            .WithColumn("ActivityName").AsString().Nullable()
            .WithColumn("NodeId").AsString().NotNullable()
            .WithColumn("EventName").AsString().Nullable()
            .WithColumn("Message").AsString().Nullable()
            .WithColumn("Source").AsString().Nullable()
            .WithColumn("SerializedActivityState").AsString().Nullable()
            .WithColumn("SerializedPayload").AsString().Nullable()
            .WithColumn("SerializedException").AsString().Nullable()
            .WithColumn("Timestamp").AsDateTimeOffset().NotNullable()
            .WithColumn("Sequence").AsInt64().NotNullable()
            ;

        IfDatabase("Sqlite")
            .Create
            .Table("WorkflowExecutionLogRecords")
            .WithColumn("Id").AsString().PrimaryKey()
            .WithColumn("WorkflowDefinitionId").AsString().NotNullable()
            .WithColumn("WorkflowDefinitionVersionId").AsString().NotNullable()
            .WithColumn("WorkflowInstanceId").AsString().NotNullable()
            .WithColumn("WorkflowVersion").AsInt32().NotNullable()
            .WithColumn("ActivityInstanceId").AsString().NotNullable()
            .WithColumn("ParentActivityInstanceId").AsString().Nullable()
            .WithColumn("ActivityId").AsString().NotNullable()
            .WithColumn("ActivityType").AsString().NotNullable()
            .WithColumn("ActivityTypeVersion").AsInt32().NotNullable()
            .WithColumn("ActivityName").AsString().Nullable()
            .WithColumn("NodeId").AsString().NotNullable()
            .WithColumn("EventName").AsString().Nullable()
            .WithColumn("Message").AsString().Nullable()
            .WithColumn("Source").AsString().Nullable()
            .WithColumn("SerializedActivityState").AsString().Nullable()
            .WithColumn("SerializedPayload").AsString().Nullable()
            .WithColumn("SerializedOutputs").AsString().Nullable()
            .WithColumn("SerializedException").AsString().Nullable()
            .WithColumn("Timestamp").AsDateTime2().NotNullable()
            .WithColumn("Sequence").AsInt64().NotNullable()
            ;
        
        IfDatabase("SqlServer", "Oracle", "MySql", "Postgres")
            .Create
            .Table("ActivityExecutionRecords")
            .WithColumn("Id").AsString().PrimaryKey()
            .WithColumn("WorkflowInstanceId").AsString().NotNullable()
            .WithColumn("ActivityId").AsString().NotNullable()
            .WithColumn("ActivityType").AsString().NotNullable()
            .WithColumn("ActivityTypeVersion").AsInt32().NotNullable()
            .WithColumn("ActivityName").AsString().Nullable()
            .WithColumn("SerializedActivityState").AsString().Nullable()
            .WithColumn("SerializedPayload").AsString().Nullable()
            .WithColumn("SerializedOutputs").AsString().Nullable()
            .WithColumn("SerializedException").AsString().Nullable()
            .WithColumn("StartedAt").AsDateTimeOffset().NotNullable()
            .WithColumn("CompletedAt").AsDateTimeOffset().Nullable()
            .WithColumn("HasBookmarks").AsBoolean().NotNullable()
            .WithColumn("Status").AsString().NotNullable()
            ;

        IfDatabase("Sqlite")
            .Create
            .Table("ActivityExecutionRecords")
            .WithColumn("Id").AsString().PrimaryKey()
            .WithColumn("WorkflowInstanceId").AsString().NotNullable()
            .WithColumn("ActivityId").AsString().NotNullable()
            .WithColumn("ActivityType").AsString().NotNullable()
            .WithColumn("ActivityTypeVersion").AsInt32().NotNullable()
            .WithColumn("ActivityName").AsString().Nullable()
            .WithColumn("SerializedActivityState").AsString().Nullable()
            .WithColumn("SerializedPayload").AsString().Nullable()
            .WithColumn("SerializedOutputs").AsString().Nullable()
            .WithColumn("SerializedException").AsString().Nullable()
            .WithColumn("StartedAt").AsDateTime2().NotNullable()
            .WithColumn("CompletedAt").AsDateTime2().Nullable()
            .WithColumn("HasBookmarks").AsBoolean().NotNullable()
            .WithColumn("Status").AsString().NotNullable()
            ;

        IfDatabase("SqlServer", "Oracle", "MySql", "Postgres")
            .Create
            .Table("WorkflowInboxMessages")
            .WithColumn("Id").AsString().PrimaryKey()
            .WithColumn("ActivityTypeName").AsString().NotNullable()
            .WithColumn("WorkflowInstanceId").AsString().Nullable()
            .WithColumn("ActivityInstanceId").AsString().Nullable()
            .WithColumn("CorrelationId").AsString().Nullable()
            .WithColumn("Hash").AsString().NotNullable()
            .WithColumn("SerializedBookmarkPayload").AsString()
            .WithColumn("SerializedInput").AsString().Nullable()
            .WithColumn("CreatedAt").AsDateTimeOffset()
            .WithColumn("ExpiresAt").AsDateTimeOffset()
            ;
        
        IfDatabase("Sqlite")
            .Create
            .Table("WorkflowInboxMessages")
            .WithColumn("Id").AsString().PrimaryKey()
            .WithColumn("ActivityTypeName").AsString().NotNullable()
            .WithColumn("WorkflowInstanceId").AsString().Nullable()
            .WithColumn("ActivityInstanceId").AsString().Nullable()
            .WithColumn("CorrelationId").AsString().Nullable()
            .WithColumn("Hash").AsString().NotNullable()
            .WithColumn("SerializedBookmarkPayload").AsString()
            .WithColumn("SerializedInput").AsString().Nullable()
            .WithColumn("CreatedAt").AsDateTime2()
            .WithColumn("ExpiresAt").AsDateTime2()
            ;
    }

    /// <inheritdoc />
    public override void Down()
    {
        Delete.Table("Triggers");
        Delete.Table("Bookmarks");
        Delete.Table("WorkflowExecutionLogRecords");
        Delete.Table("ActivityExecutionRecords");
        Delete.Table("WorkflowInboxMessages");
    }
}