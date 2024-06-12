using FluentMigrator;
using JetBrains.Annotations;
using static System.Int32;

namespace Elsa.Dapper.Migrations.Runtime;

/// <inheritdoc />
[Migration(20001, "Elsa:Runtime:V3.0")]
[PublicAPI]
public class Initial : Migration
{
    private const int MaxNodeIdColumnLength = 2048;
    /// <inheritdoc />
    public override void Up()
    {
        Create
            .Table("Triggers")
            .WithColumn("Id").AsString().PrimaryKey()
            .WithColumn("WorkflowDefinitionId").AsString().NotNullable().Indexed()
            .WithColumn("WorkflowDefinitionVersionId").AsString().NotNullable().Indexed()
            .WithColumn("Name").AsString().NotNullable().Indexed()
            .WithColumn("ActivityId").AsString().NotNullable().Indexed()
            .WithColumn("Hash").AsString().Nullable().Indexed()
            .WithColumn("SerializedPayload").AsString(MaxValue).Nullable();

        IfDatabase("SqlServer", "Oracle", "MySql", "Postgres")
            .Create
            .Table("Bookmarks")
            .WithColumn("Id").AsString().PrimaryKey()
            .WithColumn("ActivityTypeName").AsString().NotNullable().Indexed()
            .WithColumn("WorkflowInstanceId").AsString().NotNullable().Indexed()
            .WithColumn("CorrelationId").AsString().Nullable().Indexed()
            .WithColumn("ActivityInstanceId").AsString().NotNullable().Indexed()
            .WithColumn("Hash").AsString().Nullable().Indexed()
            .WithColumn("SerializedPayload").AsString(MaxValue).Nullable()
            .WithColumn("SerializedMetadata").AsString(MaxValue).Nullable()
            .WithColumn("CreatedAt").AsDateTimeOffset().NotNullable().Indexed();
        
        IfDatabase("Sqlite")
            .Create
            .Table("Bookmarks")
            .WithColumn("Id").AsString().PrimaryKey()
            .WithColumn("ActivityTypeName").AsString().NotNullable().Indexed()
            .WithColumn("WorkflowInstanceId").AsString().NotNullable().Indexed()
            .WithColumn("CorrelationId").AsString().Nullable().Indexed()
            .WithColumn("ActivityInstanceId").AsString().NotNullable().Indexed()
            .WithColumn("Hash").AsString().Nullable().Indexed()
            .WithColumn("SerializedPayload").AsString(MaxValue).Nullable()
            .WithColumn("SerializedMetadata").AsString(MaxValue).Nullable()
            .WithColumn("CreatedAt").AsDateTime2().NotNullable().Indexed();

        IfDatabase("SqlServer", "Oracle", "MySql", "Postgres")
            .Create
            .Table("WorkflowExecutionLogRecords")
            .WithColumn("Id").AsString().PrimaryKey()
            .WithColumn("WorkflowDefinitionId").AsString().NotNullable().Indexed()
            .WithColumn("WorkflowDefinitionVersionId").AsString().NotNullable().Indexed()
            .WithColumn("WorkflowInstanceId").AsString().NotNullable().Indexed()
            .WithColumn("WorkflowVersion").AsInt32().NotNullable().Indexed()
            .WithColumn("ActivityInstanceId").AsString().NotNullable().Indexed()
            .WithColumn("ParentActivityInstanceId").AsString().Nullable().Indexed()
            .WithColumn("ActivityId").AsString().NotNullable().Indexed()
            .WithColumn("ActivityType").AsString().NotNullable().Indexed()
            .WithColumn("ActivityTypeVersion").AsInt32().NotNullable().Indexed()
            .WithColumn("ActivityName").AsString().Nullable().Indexed()
            .WithColumn("ActivityNodeId").AsString(MaxNodeIdColumnLength).NotNullable().Indexed()
            .WithColumn("EventName").AsString().Nullable().Indexed()
            .WithColumn("Message").AsString(MaxValue).Nullable()
            .WithColumn("Source").AsString().Nullable()
            .WithColumn("SerializedActivityState").AsString(MaxValue).Nullable()
            .WithColumn("SerializedPayload").AsString(MaxValue).Nullable()
            .WithColumn("SerializedException").AsString(MaxValue).Nullable()
            .WithColumn("Timestamp").AsDateTimeOffset().NotNullable().Indexed()
            .WithColumn("Sequence").AsInt64().NotNullable().Indexed()
            ;

        IfDatabase("Sqlite")
            .Create
            .Table("WorkflowExecutionLogRecords")
            .WithColumn("Id").AsString().PrimaryKey()
            .WithColumn("WorkflowDefinitionId").AsString().NotNullable().Indexed()
            .WithColumn("WorkflowDefinitionVersionId").AsString().NotNullable().Indexed()
            .WithColumn("WorkflowInstanceId").AsString().NotNullable().Indexed()
            .WithColumn("WorkflowVersion").AsInt32().NotNullable().Indexed()
            .WithColumn("ActivityInstanceId").AsString().NotNullable().Indexed()
            .WithColumn("ParentActivityInstanceId").AsString().Nullable().Indexed()
            .WithColumn("ActivityId").AsString().NotNullable().Indexed()
            .WithColumn("ActivityType").AsString().NotNullable().Indexed()
            .WithColumn("ActivityTypeVersion").AsInt32().NotNullable().Indexed()
            .WithColumn("ActivityName").AsString().Nullable().Indexed()
            .WithColumn("ActivityNodeId").AsString(MaxNodeIdColumnLength).NotNullable().Indexed()
            .WithColumn("EventName").AsString().Nullable().Indexed()
            .WithColumn("Message").AsString(MaxValue).Nullable()
            .WithColumn("Source").AsString().Nullable()
            .WithColumn("SerializedActivityState").AsString(MaxValue).Nullable()
            .WithColumn("SerializedPayload").AsString(MaxValue).Nullable()
            .WithColumn("SerializedOutputs").AsString(MaxValue).Nullable()
            .WithColumn("SerializedException").AsString(MaxValue).Nullable()
            .WithColumn("Timestamp").AsDateTime2().NotNullable().Indexed()
            .WithColumn("Sequence").AsInt64().NotNullable().Indexed()
            ;
        
        IfDatabase("SqlServer", "Oracle", "MySql", "Postgres")
            .Create
            .Table("ActivityExecutionRecords")
            .WithColumn("Id").AsString().PrimaryKey()
            .WithColumn("WorkflowInstanceId").AsString().NotNullable().Indexed()
            .WithColumn("ActivityId").AsString().NotNullable().Indexed()
            .WithColumn("ActivityNodeId").AsString(MaxNodeIdColumnLength).NotNullable().Indexed()
            .WithColumn("ActivityType").AsString().NotNullable().Indexed()
            .WithColumn("ActivityTypeVersion").AsInt32().NotNullable().Indexed()
            .WithColumn("ActivityName").AsString().Nullable().Indexed()
            .WithColumn("SerializedActivityState").AsString(MaxValue).Nullable()
            .WithColumn("SerializedPayload").AsString(MaxValue).Nullable()
            .WithColumn("SerializedOutputs").AsString(MaxValue).Nullable()
            .WithColumn("SerializedException").AsString(MaxValue).Nullable()
            .WithColumn("StartedAt").AsDateTimeOffset().NotNullable().Indexed()
            .WithColumn("CompletedAt").AsDateTimeOffset().Nullable().Indexed()
            .WithColumn("HasBookmarks").AsBoolean().NotNullable().Indexed()
            .WithColumn("Status").AsString().NotNullable().Indexed()
            ;

        IfDatabase("Sqlite")
            .Create
            .Table("ActivityExecutionRecords")
            .WithColumn("Id").AsString().PrimaryKey()
            .WithColumn("WorkflowInstanceId").AsString().NotNullable().Indexed()
            .WithColumn("ActivityId").AsString().NotNullable().Indexed()
            .WithColumn("ActivityNodeId").AsString(MaxNodeIdColumnLength).NotNullable().Indexed()
            .WithColumn("ActivityType").AsString().NotNullable().Indexed()
            .WithColumn("ActivityTypeVersion").AsInt32().NotNullable().Indexed()
            .WithColumn("ActivityName").AsString().Nullable().Indexed()
            .WithColumn("SerializedActivityState").AsString(MaxValue).Nullable()
            .WithColumn("SerializedPayload").AsString(MaxValue).Nullable()
            .WithColumn("SerializedOutputs").AsString(MaxValue).Nullable()
            .WithColumn("SerializedException").AsString(MaxValue).Nullable()
            .WithColumn("StartedAt").AsDateTime2().NotNullable().Indexed()
            .WithColumn("CompletedAt").AsDateTime2().Nullable().Indexed()
            .WithColumn("HasBookmarks").AsBoolean().NotNullable().Indexed()
            .WithColumn("Status").AsString().NotNullable().Indexed()
            ;

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