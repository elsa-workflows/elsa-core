IF OBJECT_ID(N'[Elsa].[__EFMigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'Elsa') IS NULL EXEC(N'CREATE SCHEMA [Elsa];');
    CREATE TABLE [Elsa].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF SCHEMA_ID(N'Elsa') IS NULL EXEC(N'CREATE SCHEMA [Elsa];');
GO

CREATE TABLE [Elsa].[ActivityExecutionRecords] (
    [Id] nvarchar(450) NOT NULL,
    [WorkflowInstanceId] nvarchar(450) NOT NULL,
    [ActivityId] nvarchar(450) NOT NULL,
    [ActivityNodeId] nvarchar(450) NOT NULL,
    [ActivityType] nvarchar(450) NOT NULL,
    [ActivityTypeVersion] int NOT NULL,
    [ActivityName] nvarchar(450) NULL,
    [StartedAt] datetimeoffset NOT NULL,
    [HasBookmarks] bit NOT NULL,
    [Status] nvarchar(450) NOT NULL,
    [CompletedAt] datetimeoffset NULL,
    [SerializedActivityState] nvarchar(max) NULL,
    [SerializedException] nvarchar(max) NULL,
    [SerializedOutputs] nvarchar(max) NULL,
    [SerializedPayload] nvarchar(max) NULL,
    CONSTRAINT [PK_ActivityExecutionRecords] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Elsa].[Bookmarks] (
    [BookmarkId] nvarchar(450) NOT NULL,
    [ActivityTypeName] nvarchar(450) NOT NULL,
    [Hash] nvarchar(450) NOT NULL,
    [WorkflowInstanceId] nvarchar(450) NOT NULL,
    [ActivityInstanceId] nvarchar(450) NULL,
    [CorrelationId] nvarchar(max) NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [SerializedMetadata] nvarchar(max) NULL,
    [SerializedPayload] nvarchar(max) NULL,
    CONSTRAINT [PK_Bookmarks] PRIMARY KEY ([BookmarkId])
);
GO

CREATE TABLE [Elsa].[Triggers] (
    [Id] nvarchar(450) NOT NULL,
    [WorkflowDefinitionId] nvarchar(450) NOT NULL,
    [WorkflowDefinitionVersionId] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [ActivityId] nvarchar(max) NOT NULL,
    [Hash] nvarchar(450) NULL,
    [SerializedPayload] nvarchar(max) NULL,
    CONSTRAINT [PK_Triggers] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Elsa].[WorkflowExecutionLogRecords] (
    [Id] nvarchar(450) NOT NULL,
    [WorkflowDefinitionId] nvarchar(450) NOT NULL,
    [WorkflowDefinitionVersionId] nvarchar(450) NOT NULL,
    [WorkflowInstanceId] nvarchar(450) NOT NULL,
    [WorkflowVersion] int NOT NULL,
    [ActivityInstanceId] nvarchar(450) NOT NULL,
    [ParentActivityInstanceId] nvarchar(450) NULL,
    [ActivityId] nvarchar(450) NOT NULL,
    [ActivityType] nvarchar(450) NOT NULL,
    [ActivityTypeVersion] int NOT NULL,
    [ActivityName] nvarchar(450) NULL,
    [ActivityNodeId] nvarchar(450) NOT NULL,
    [Timestamp] datetimeoffset NOT NULL,
    [Sequence] bigint NOT NULL,
    [EventName] nvarchar(450) NULL,
    [Message] nvarchar(max) NULL,
    [Source] nvarchar(max) NULL,
    [SerializedActivityState] nvarchar(max) NULL,
    [SerializedPayload] nvarchar(max) NULL,
    CONSTRAINT [PK_WorkflowExecutionLogRecords] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Elsa].[WorkflowInboxMessages] (
    [Id] nvarchar(450) NOT NULL,
    [ActivityTypeName] nvarchar(450) NOT NULL,
    [Hash] nvarchar(450) NOT NULL,
    [WorkflowInstanceId] nvarchar(450) NULL,
    [CorrelationId] nvarchar(450) NULL,
    [ActivityInstanceId] nvarchar(450) NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [ExpiresAt] datetimeoffset NOT NULL,
    [SerializedBookmarkPayload] nvarchar(max) NULL,
    [SerializedInput] nvarchar(max) NULL,
    CONSTRAINT [PK_WorkflowInboxMessages] PRIMARY KEY ([Id])
);
GO

CREATE INDEX [IX_ActivityExecutionRecord_ActivityId] ON [Elsa].[ActivityExecutionRecords] ([ActivityId]);
GO

CREATE INDEX [IX_ActivityExecutionRecord_ActivityName] ON [Elsa].[ActivityExecutionRecords] ([ActivityName]);
GO

CREATE INDEX [IX_ActivityExecutionRecord_ActivityNodeId] ON [Elsa].[ActivityExecutionRecords] ([ActivityNodeId]);
GO

CREATE INDEX [IX_ActivityExecutionRecord_ActivityType] ON [Elsa].[ActivityExecutionRecords] ([ActivityType]);
GO

CREATE INDEX [IX_ActivityExecutionRecord_ActivityType_ActivityTypeVersion] ON [Elsa].[ActivityExecutionRecords] ([ActivityType], [ActivityTypeVersion]);
GO

CREATE INDEX [IX_ActivityExecutionRecord_ActivityTypeVersion] ON [Elsa].[ActivityExecutionRecords] ([ActivityTypeVersion]);
GO

CREATE INDEX [IX_ActivityExecutionRecord_CompletedAt] ON [Elsa].[ActivityExecutionRecords] ([CompletedAt]);
GO

CREATE INDEX [IX_ActivityExecutionRecord_HasBookmarks] ON [Elsa].[ActivityExecutionRecords] ([HasBookmarks]);
GO

CREATE INDEX [IX_ActivityExecutionRecord_StartedAt] ON [Elsa].[ActivityExecutionRecords] ([StartedAt]);
GO

CREATE INDEX [IX_ActivityExecutionRecord_Status] ON [Elsa].[ActivityExecutionRecords] ([Status]);
GO

CREATE INDEX [IX_ActivityExecutionRecord_WorkflowInstanceId] ON [Elsa].[ActivityExecutionRecords] ([WorkflowInstanceId]);
GO

CREATE INDEX [IX_StoredBookmark_ActivityInstanceId] ON [Elsa].[Bookmarks] ([ActivityInstanceId]);
GO

CREATE INDEX [IX_StoredBookmark_ActivityTypeName] ON [Elsa].[Bookmarks] ([ActivityTypeName]);
GO

CREATE INDEX [IX_StoredBookmark_ActivityTypeName_Hash] ON [Elsa].[Bookmarks] ([ActivityTypeName], [Hash]);
GO

CREATE INDEX [IX_StoredBookmark_ActivityTypeName_Hash_WorkflowInstanceId] ON [Elsa].[Bookmarks] ([ActivityTypeName], [Hash], [WorkflowInstanceId]);
GO

CREATE INDEX [IX_StoredBookmark_CreatedAt] ON [Elsa].[Bookmarks] ([CreatedAt]);
GO

CREATE INDEX [IX_StoredBookmark_Hash] ON [Elsa].[Bookmarks] ([Hash]);
GO

CREATE INDEX [IX_StoredBookmark_WorkflowInstanceId] ON [Elsa].[Bookmarks] ([WorkflowInstanceId]);
GO

CREATE INDEX [IX_StoredTrigger_Hash] ON [Elsa].[Triggers] ([Hash]);
GO

CREATE INDEX [IX_StoredTrigger_Name] ON [Elsa].[Triggers] ([Name]);
GO

CREATE INDEX [IX_StoredTrigger_WorkflowDefinitionId] ON [Elsa].[Triggers] ([WorkflowDefinitionId]);
GO

CREATE INDEX [IX_StoredTrigger_WorkflowDefinitionVersionId] ON [Elsa].[Triggers] ([WorkflowDefinitionVersionId]);
GO

CREATE INDEX [IX_WorkflowExecutionLogRecord_ActivityId] ON [Elsa].[WorkflowExecutionLogRecords] ([ActivityId]);
GO

CREATE INDEX [IX_WorkflowExecutionLogRecord_ActivityInstanceId] ON [Elsa].[WorkflowExecutionLogRecords] ([ActivityInstanceId]);
GO

CREATE INDEX [IX_WorkflowExecutionLogRecord_ActivityName] ON [Elsa].[WorkflowExecutionLogRecords] ([ActivityName]);
GO

CREATE INDEX [IX_WorkflowExecutionLogRecord_ActivityNodeId] ON [Elsa].[WorkflowExecutionLogRecords] ([ActivityNodeId]);
GO

CREATE INDEX [IX_WorkflowExecutionLogRecord_ActivityType] ON [Elsa].[WorkflowExecutionLogRecords] ([ActivityType]);
GO

CREATE INDEX [IX_WorkflowExecutionLogRecord_ActivityType_ActivityTypeVersion] ON [Elsa].[WorkflowExecutionLogRecords] ([ActivityType], [ActivityTypeVersion]);
GO

CREATE INDEX [IX_WorkflowExecutionLogRecord_ActivityTypeVersion] ON [Elsa].[WorkflowExecutionLogRecords] ([ActivityTypeVersion]);
GO

CREATE INDEX [IX_WorkflowExecutionLogRecord_EventName] ON [Elsa].[WorkflowExecutionLogRecords] ([EventName]);
GO

CREATE INDEX [IX_WorkflowExecutionLogRecord_ParentActivityInstanceId] ON [Elsa].[WorkflowExecutionLogRecords] ([ParentActivityInstanceId]);
GO

CREATE INDEX [IX_WorkflowExecutionLogRecord_Sequence] ON [Elsa].[WorkflowExecutionLogRecords] ([Sequence]);
GO

CREATE INDEX [IX_WorkflowExecutionLogRecord_Timestamp] ON [Elsa].[WorkflowExecutionLogRecords] ([Timestamp]);
GO

CREATE INDEX [IX_WorkflowExecutionLogRecord_Timestamp_Sequence] ON [Elsa].[WorkflowExecutionLogRecords] ([Timestamp], [Sequence]);
GO

CREATE INDEX [IX_WorkflowExecutionLogRecord_WorkflowDefinitionId] ON [Elsa].[WorkflowExecutionLogRecords] ([WorkflowDefinitionId]);
GO

CREATE INDEX [IX_WorkflowExecutionLogRecord_WorkflowDefinitionVersionId] ON [Elsa].[WorkflowExecutionLogRecords] ([WorkflowDefinitionVersionId]);
GO

CREATE INDEX [IX_WorkflowExecutionLogRecord_WorkflowInstanceId] ON [Elsa].[WorkflowExecutionLogRecords] ([WorkflowInstanceId]);
GO

CREATE INDEX [IX_WorkflowExecutionLogRecord_WorkflowVersion] ON [Elsa].[WorkflowExecutionLogRecords] ([WorkflowVersion]);
GO

CREATE INDEX [IX_WorkflowInboxMessage_ActivityInstanceId] ON [Elsa].[WorkflowInboxMessages] ([ActivityInstanceId]);
GO

CREATE INDEX [IX_WorkflowInboxMessage_ActivityTypeName] ON [Elsa].[WorkflowInboxMessages] ([ActivityTypeName]);
GO

CREATE INDEX [IX_WorkflowInboxMessage_CorrelationId] ON [Elsa].[WorkflowInboxMessages] ([CorrelationId]);
GO

CREATE INDEX [IX_WorkflowInboxMessage_CreatedAt] ON [Elsa].[WorkflowInboxMessages] ([CreatedAt]);
GO

CREATE INDEX [IX_WorkflowInboxMessage_ExpiresAt] ON [Elsa].[WorkflowInboxMessages] ([ExpiresAt]);
GO

CREATE INDEX [IX_WorkflowInboxMessage_Hash] ON [Elsa].[WorkflowInboxMessages] ([Hash]);
GO

CREATE INDEX [IX_WorkflowInboxMessage_WorkflowInstanceId] ON [Elsa].[WorkflowInboxMessages] ([WorkflowInstanceId]);
GO

INSERT INTO [Elsa].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20231024160944_Initial', N'7.0.14');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Elsa].[ActivityExecutionRecords] ADD [SerializedActivityStateCompressionAlgorithm] nvarchar(max) NULL;
GO

ALTER TABLE [Elsa].[ActivityExecutionRecords] ADD [SerializedProperties] nvarchar(max) NULL;
GO

CREATE TABLE [Elsa].[KeyValuePairs] (
    [Key] nvarchar(450) NOT NULL,
    [SerializedValue] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_KeyValuePairs] PRIMARY KEY ([Key])
);
GO

INSERT INTO [Elsa].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20240312145137_V3_1', N'7.0.14');
GO

COMMIT;
GO

