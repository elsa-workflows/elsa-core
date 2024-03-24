CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;

CREATE TABLE "ActivityExecutionRecords" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_ActivityExecutionRecords" PRIMARY KEY,
    "WorkflowInstanceId" TEXT NOT NULL,
    "ActivityId" TEXT NOT NULL,
    "ActivityNodeId" TEXT NOT NULL,
    "ActivityType" TEXT NOT NULL,
    "ActivityTypeVersion" INTEGER NOT NULL,
    "ActivityName" TEXT NULL,
    "StartedAt" TEXT NOT NULL,
    "HasBookmarks" INTEGER NOT NULL,
    "Status" TEXT NOT NULL,
    "CompletedAt" TEXT NULL,
    "SerializedActivityState" TEXT NULL,
    "SerializedException" TEXT NULL,
    "SerializedOutputs" TEXT NULL,
    "SerializedPayload" TEXT NULL
);

CREATE TABLE "Bookmarks" (
    "BookmarkId" TEXT NOT NULL CONSTRAINT "PK_Bookmarks" PRIMARY KEY,
    "ActivityTypeName" TEXT NOT NULL,
    "Hash" TEXT NOT NULL,
    "WorkflowInstanceId" TEXT NOT NULL,
    "ActivityInstanceId" TEXT NULL,
    "CorrelationId" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    "SerializedMetadata" TEXT NULL,
    "SerializedPayload" TEXT NULL
);

CREATE TABLE "Triggers" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Triggers" PRIMARY KEY,
    "WorkflowDefinitionId" TEXT NOT NULL,
    "WorkflowDefinitionVersionId" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "ActivityId" TEXT NOT NULL,
    "Hash" TEXT NULL,
    "SerializedPayload" TEXT NULL
);

CREATE TABLE "WorkflowExecutionLogRecords" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_WorkflowExecutionLogRecords" PRIMARY KEY,
    "WorkflowDefinitionId" TEXT NOT NULL,
    "WorkflowDefinitionVersionId" TEXT NOT NULL,
    "WorkflowInstanceId" TEXT NOT NULL,
    "WorkflowVersion" INTEGER NOT NULL,
    "ActivityInstanceId" TEXT NOT NULL,
    "ParentActivityInstanceId" TEXT NULL,
    "ActivityId" TEXT NOT NULL,
    "ActivityType" TEXT NOT NULL,
    "ActivityTypeVersion" INTEGER NOT NULL,
    "ActivityName" TEXT NULL,
    "ActivityNodeId" TEXT NOT NULL,
    "Timestamp" TEXT NOT NULL,
    "Sequence" INTEGER NOT NULL,
    "EventName" TEXT NULL,
    "Message" TEXT NULL,
    "Source" TEXT NULL,
    "SerializedActivityState" TEXT NULL,
    "SerializedPayload" TEXT NULL
);

CREATE TABLE "WorkflowInboxMessages" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_WorkflowInboxMessages" PRIMARY KEY,
    "ActivityTypeName" TEXT NOT NULL,
    "Hash" TEXT NOT NULL,
    "WorkflowInstanceId" TEXT NULL,
    "CorrelationId" TEXT NULL,
    "ActivityInstanceId" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    "ExpiresAt" TEXT NOT NULL,
    "SerializedBookmarkPayload" TEXT NULL,
    "SerializedInput" TEXT NULL
);

CREATE INDEX "IX_ActivityExecutionRecord_ActivityId" ON "ActivityExecutionRecords" ("ActivityId");

CREATE INDEX "IX_ActivityExecutionRecord_ActivityName" ON "ActivityExecutionRecords" ("ActivityName");

CREATE INDEX "IX_ActivityExecutionRecord_ActivityNodeId" ON "ActivityExecutionRecords" ("ActivityNodeId");

CREATE INDEX "IX_ActivityExecutionRecord_ActivityType" ON "ActivityExecutionRecords" ("ActivityType");

CREATE INDEX "IX_ActivityExecutionRecord_ActivityType_ActivityTypeVersion" ON "ActivityExecutionRecords" ("ActivityType", "ActivityTypeVersion");

CREATE INDEX "IX_ActivityExecutionRecord_ActivityTypeVersion" ON "ActivityExecutionRecords" ("ActivityTypeVersion");

CREATE INDEX "IX_ActivityExecutionRecord_CompletedAt" ON "ActivityExecutionRecords" ("CompletedAt");

CREATE INDEX "IX_ActivityExecutionRecord_HasBookmarks" ON "ActivityExecutionRecords" ("HasBookmarks");

CREATE INDEX "IX_ActivityExecutionRecord_StartedAt" ON "ActivityExecutionRecords" ("StartedAt");

CREATE INDEX "IX_ActivityExecutionRecord_Status" ON "ActivityExecutionRecords" ("Status");

CREATE INDEX "IX_ActivityExecutionRecord_WorkflowInstanceId" ON "ActivityExecutionRecords" ("WorkflowInstanceId");

CREATE INDEX "IX_StoredBookmark_ActivityInstanceId" ON "Bookmarks" ("ActivityInstanceId");

CREATE INDEX "IX_StoredBookmark_ActivityTypeName" ON "Bookmarks" ("ActivityTypeName");

CREATE INDEX "IX_StoredBookmark_ActivityTypeName_Hash" ON "Bookmarks" ("ActivityTypeName", "Hash");

CREATE INDEX "IX_StoredBookmark_ActivityTypeName_Hash_WorkflowInstanceId" ON "Bookmarks" ("ActivityTypeName", "Hash", "WorkflowInstanceId");

CREATE INDEX "IX_StoredBookmark_CreatedAt" ON "Bookmarks" ("CreatedAt");

CREATE INDEX "IX_StoredBookmark_Hash" ON "Bookmarks" ("Hash");

CREATE INDEX "IX_StoredBookmark_WorkflowInstanceId" ON "Bookmarks" ("WorkflowInstanceId");

CREATE INDEX "IX_StoredTrigger_Hash" ON "Triggers" ("Hash");

CREATE INDEX "IX_StoredTrigger_Name" ON "Triggers" ("Name");

CREATE INDEX "IX_StoredTrigger_WorkflowDefinitionId" ON "Triggers" ("WorkflowDefinitionId");

CREATE INDEX "IX_StoredTrigger_WorkflowDefinitionVersionId" ON "Triggers" ("WorkflowDefinitionVersionId");

CREATE INDEX "IX_WorkflowExecutionLogRecord_ActivityId" ON "WorkflowExecutionLogRecords" ("ActivityId");

CREATE INDEX "IX_WorkflowExecutionLogRecord_ActivityInstanceId" ON "WorkflowExecutionLogRecords" ("ActivityInstanceId");

CREATE INDEX "IX_WorkflowExecutionLogRecord_ActivityName" ON "WorkflowExecutionLogRecords" ("ActivityName");

CREATE INDEX "IX_WorkflowExecutionLogRecord_ActivityNodeId" ON "WorkflowExecutionLogRecords" ("ActivityNodeId");

CREATE INDEX "IX_WorkflowExecutionLogRecord_ActivityType" ON "WorkflowExecutionLogRecords" ("ActivityType");

CREATE INDEX "IX_WorkflowExecutionLogRecord_ActivityType_ActivityTypeVersion" ON "WorkflowExecutionLogRecords" ("ActivityType", "ActivityTypeVersion");

CREATE INDEX "IX_WorkflowExecutionLogRecord_ActivityTypeVersion" ON "WorkflowExecutionLogRecords" ("ActivityTypeVersion");

CREATE INDEX "IX_WorkflowExecutionLogRecord_EventName" ON "WorkflowExecutionLogRecords" ("EventName");

CREATE INDEX "IX_WorkflowExecutionLogRecord_ParentActivityInstanceId" ON "WorkflowExecutionLogRecords" ("ParentActivityInstanceId");

CREATE INDEX "IX_WorkflowExecutionLogRecord_Sequence" ON "WorkflowExecutionLogRecords" ("Sequence");

CREATE INDEX "IX_WorkflowExecutionLogRecord_Timestamp" ON "WorkflowExecutionLogRecords" ("Timestamp");

CREATE INDEX "IX_WorkflowExecutionLogRecord_Timestamp_Sequence" ON "WorkflowExecutionLogRecords" ("Timestamp", "Sequence");

CREATE INDEX "IX_WorkflowExecutionLogRecord_WorkflowDefinitionId" ON "WorkflowExecutionLogRecords" ("WorkflowDefinitionId");

CREATE INDEX "IX_WorkflowExecutionLogRecord_WorkflowDefinitionVersionId" ON "WorkflowExecutionLogRecords" ("WorkflowDefinitionVersionId");

CREATE INDEX "IX_WorkflowExecutionLogRecord_WorkflowInstanceId" ON "WorkflowExecutionLogRecords" ("WorkflowInstanceId");

CREATE INDEX "IX_WorkflowExecutionLogRecord_WorkflowVersion" ON "WorkflowExecutionLogRecords" ("WorkflowVersion");

CREATE INDEX "IX_WorkflowInboxMessage_ActivityInstanceId" ON "WorkflowInboxMessages" ("ActivityInstanceId");

CREATE INDEX "IX_WorkflowInboxMessage_ActivityTypeName" ON "WorkflowInboxMessages" ("ActivityTypeName");

CREATE INDEX "IX_WorkflowInboxMessage_CorrelationId" ON "WorkflowInboxMessages" ("CorrelationId");

CREATE INDEX "IX_WorkflowInboxMessage_CreatedAt" ON "WorkflowInboxMessages" ("CreatedAt");

CREATE INDEX "IX_WorkflowInboxMessage_ExpiresAt" ON "WorkflowInboxMessages" ("ExpiresAt");

CREATE INDEX "IX_WorkflowInboxMessage_Hash" ON "WorkflowInboxMessages" ("Hash");

CREATE INDEX "IX_WorkflowInboxMessage_WorkflowInstanceId" ON "WorkflowInboxMessages" ("WorkflowInstanceId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20231024160948_Initial', '7.0.14');

COMMIT;

BEGIN TRANSACTION;

ALTER TABLE "ActivityExecutionRecords" ADD "SerializedActivityStateCompressionAlgorithm" TEXT NULL;

ALTER TABLE "ActivityExecutionRecords" ADD "SerializedProperties" TEXT NULL;

CREATE TABLE "KeyValuePairs" (
    "Key" TEXT NOT NULL CONSTRAINT "PK_KeyValuePairs" PRIMARY KEY,
    "SerializedValue" TEXT NOT NULL
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20240312145142_V3_1', '7.0.14');

COMMIT;

