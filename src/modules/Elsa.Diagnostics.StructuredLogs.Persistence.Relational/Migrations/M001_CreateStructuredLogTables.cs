using FluentMigrator;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Migrations;

[Migration(2026051301)]
public class M001CreateStructuredLogTables : Migration
{
    private const string TableName = "StructuredLogEvents";

    public override void Up()
    {
        Create.Table(TableName)
            .WithColumn("Id").AsString(64).PrimaryKey()
            .WithColumn("Sequence").AsInt64().NotNullable()
            .WithColumn("Timestamp").AsString(40).NotNullable()
            .WithColumn("ReceivedAt").AsString(40).NotNullable()
            .WithColumn("Level").AsInt32().NotNullable()
            .WithColumn("Category").AsString(512).NotNullable()
            .WithColumn("EventId").AsInt32().NotNullable()
            .WithColumn("EventName").AsString(512).Nullable()
            .WithColumn("Message").AsString(int.MaxValue).NotNullable()
            .WithColumn("MessageTemplate").AsString(int.MaxValue).Nullable()
            .WithColumn("ExceptionJson").AsString(int.MaxValue).Nullable()
            .WithColumn("ScopesJson").AsString(int.MaxValue).NotNullable()
            .WithColumn("PropertiesJson").AsString(int.MaxValue).NotNullable()
            .WithColumn("TraceId").AsString(64).Nullable()
            .WithColumn("SpanId").AsString(64).Nullable()
            .WithColumn("CorrelationId").AsString(256).Nullable()
            .WithColumn("TenantId").AsString(256).Nullable()
            .WithColumn("WorkflowDefinitionId").AsString(256).Nullable()
            .WithColumn("WorkflowInstanceId").AsString(256).Nullable()
            .WithColumn("SourceId").AsString(512).NotNullable();

        Create.Index("IX_StructuredLogEvents_Timestamp").OnTable(TableName).OnColumn("Timestamp").Descending();
        Create.Index("IX_StructuredLogEvents_ReceivedAt").OnTable(TableName).OnColumn("ReceivedAt").Descending();
        Create.Index("IX_StructuredLogEvents_Level").OnTable(TableName).OnColumn("Level").Ascending();
        Create.Index("IX_StructuredLogEvents_Category").OnTable(TableName).OnColumn("Category").Ascending();
        Create.Index("IX_StructuredLogEvents_SourceId").OnTable(TableName).OnColumn("SourceId").Ascending();
        Create.Index("IX_StructuredLogEvents_TenantId").OnTable(TableName).OnColumn("TenantId").Ascending();
        Create.Index("IX_StructuredLogEvents_WorkflowDefinitionId").OnTable(TableName).OnColumn("WorkflowDefinitionId").Ascending();
        Create.Index("IX_StructuredLogEvents_WorkflowInstanceId").OnTable(TableName).OnColumn("WorkflowInstanceId").Ascending();
        Create.Index("IX_StructuredLogEvents_CorrelationId").OnTable(TableName).OnColumn("CorrelationId").Ascending();
        Create.Index("IX_StructuredLogEvents_TraceId").OnTable(TableName).OnColumn("TraceId").Ascending();
    }

    public override void Down()
    {
        Delete.Table(TableName);
    }
}
