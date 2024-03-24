CREATE TABLE IF NOT EXISTS "Elsa"."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

CREATE TABLE "Elsa"."ActivityExecutionRecords" (
    "Id" text NOT NULL,
    "WorkflowInstanceId" text NOT NULL,
    "ActivityId" text NOT NULL,
    "ActivityNodeId" text NOT NULL,
    "ActivityType" text NOT NULL,
    "ActivityTypeVersion" integer NOT NULL,
    "ActivityName" text NULL,
    "StartedAt" timestamp with time zone NOT NULL,
    "HasBookmarks" boolean NOT NULL,
    "Status" text NOT NULL,
    "CompletedAt" timestamp with time zone NULL,
    "SerializedActivityState" text NULL,
    "SerializedException" text NULL,
    "SerializedOutputs" text NULL,
    "SerializedPayload" text NULL,
    CONSTRAINT "PK_ActivityExecutionRecords" PRIMARY KEY ("Id")
);

CREATE TABLE "Elsa"."Bookmarks" (
    "BookmarkId" text NOT NULL,
    "ActivityTypeName" text NOT NULL,
    "Hash" text NOT NULL,
    "WorkflowInstanceId" text NOT NULL,
    "ActivityInstanceId" text NULL,
    "CorrelationId" text NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "SerializedMetadata" text NULL,
    "SerializedPayload" text NULL,
    CONSTRAINT "PK_Bookmarks" PRIMARY KEY ("BookmarkId")
);

CREATE TABLE "Elsa"."Triggers" (
    "Id" text NOT NULL,
    "WorkflowDefinitionId" text NOT NULL,
    "WorkflowDefinitionVersionId" text NOT NULL,
    "Name" text NOT NULL,
    "ActivityId" text NOT NULL,
    "Hash" text NULL,
    "SerializedPayload" text NULL,
    CONSTRAINT "PK_Triggers" PRIMARY KEY ("Id")
);

CREATE TABLE "Elsa"."WorkflowExecutionLogRecords" (
    "Id" text NOT NULL,
    "WorkflowDefinitionId" text NOT NULL,
    "WorkflowDefinitionVersionId" text NOT NULL,
    "WorkflowInstanceId" text NOT NULL,
    "WorkflowVersion" integer NOT NULL,
    "ActivityInstanceId" text NOT NULL,
    "ParentActivityInstanceId" text NULL,
    "ActivityId" text NOT NULL,
    "ActivityType" text NOT NULL,
    "ActivityTypeVersion" integer NOT NULL,
    "ActivityName" text NULL,
    "ActivityNodeId" text NOT NULL,
    "Timestamp" timestamp with time zone NOT NULL,
    "Sequence" bigint NOT NULL,
    "EventName" text NULL,
    "Message" text NULL,
    "Source" text NULL,
    "SerializedActivityState" text NULL,
    "SerializedPayload" text NULL,
    CONSTRAINT "PK_WorkflowExecutionLogRecords" PRIMARY KEY ("Id")
);

CREATE TABLE "Elsa"."WorkflowInboxMessages" (
    "Id" text NOT NULL,
    "ActivityTypeName" text NOT NULL,
    "Hash" text NOT NULL,
    "WorkflowInstanceId" text NULL,
    "CorrelationId" text NULL,
    "ActivityInstanceId" text NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "SerializedBookmarkPayload" text NULL,
    "SerializedInput" text NULL,
    CONSTRAINT "PK_WorkflowInboxMessages" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_ActivityExecutionRecord_ActivityId" ON "Elsa"."ActivityExecutionRecords" ("ActivityId");

CREATE INDEX "IX_ActivityExecutionRecord_ActivityName" ON "Elsa"."ActivityExecutionRecords" ("ActivityName");

CREATE INDEX "IX_ActivityExecutionRecord_ActivityNodeId" ON "Elsa"."ActivityExecutionRecords" ("ActivityNodeId");

CREATE INDEX "IX_ActivityExecutionRecord_ActivityType" ON "Elsa"."ActivityExecutionRecords" ("ActivityType");

CREATE INDEX "IX_ActivityExecutionRecord_ActivityType_ActivityTypeVersion" ON "Elsa"."ActivityExecutionRecords" ("ActivityType", "ActivityTypeVersion");

CREATE INDEX "IX_ActivityExecutionRecord_ActivityTypeVersion" ON "Elsa"."ActivityExecutionRecords" ("ActivityTypeVersion");

CREATE INDEX "IX_ActivityExecutionRecord_CompletedAt" ON "Elsa"."ActivityExecutionRecords" ("CompletedAt");

CREATE INDEX "IX_ActivityExecutionRecord_HasBookmarks" ON "Elsa"."ActivityExecutionRecords" ("HasBookmarks");

CREATE INDEX "IX_ActivityExecutionRecord_StartedAt" ON "Elsa"."ActivityExecutionRecords" ("StartedAt");

CREATE INDEX "IX_ActivityExecutionRecord_Status" ON "Elsa"."ActivityExecutionRecords" ("Status");

CREATE INDEX "IX_ActivityExecutionRecord_WorkflowInstanceId" ON "Elsa"."ActivityExecutionRecords" ("WorkflowInstanceId");

CREATE INDEX "IX_StoredBookmark_ActivityInstanceId" ON "Elsa"."Bookmarks" ("ActivityInstanceId");

CREATE INDEX "IX_StoredBookmark_ActivityTypeName" ON "Elsa"."Bookmarks" ("ActivityTypeName");

CREATE INDEX "IX_StoredBookmark_ActivityTypeName_Hash" ON "Elsa"."Bookmarks" ("ActivityTypeName", "Hash");

CREATE INDEX "IX_StoredBookmark_ActivityTypeName_Hash_WorkflowInstanceId" ON "Elsa"."Bookmarks" ("ActivityTypeName", "Hash", "WorkflowInstanceId");

CREATE INDEX "IX_StoredBookmark_CreatedAt" ON "Elsa"."Bookmarks" ("CreatedAt");

CREATE INDEX "IX_StoredBookmark_Hash" ON "Elsa"."Bookmarks" ("Hash");

CREATE INDEX "IX_StoredBookmark_WorkflowInstanceId" ON "Elsa"."Bookmarks" ("WorkflowInstanceId");

CREATE INDEX "IX_StoredTrigger_Hash" ON "Elsa"."Triggers" ("Hash");

CREATE INDEX "IX_StoredTrigger_Name" ON "Elsa"."Triggers" ("Name");

CREATE INDEX "IX_StoredTrigger_WorkflowDefinitionId" ON "Elsa"."Triggers" ("WorkflowDefinitionId");

CREATE INDEX "IX_StoredTrigger_WorkflowDefinitionVersionId" ON "Elsa"."Triggers" ("WorkflowDefinitionVersionId");

CREATE INDEX "IX_WorkflowExecutionLogRecord_ActivityId" ON "Elsa"."WorkflowExecutionLogRecords" ("ActivityId");

CREATE INDEX "IX_WorkflowExecutionLogRecord_ActivityInstanceId" ON "Elsa"."WorkflowExecutionLogRecords" ("ActivityInstanceId");

CREATE INDEX "IX_WorkflowExecutionLogRecord_ActivityName" ON "Elsa"."WorkflowExecutionLogRecords" ("ActivityName");

CREATE INDEX "IX_WorkflowExecutionLogRecord_ActivityNodeId" ON "Elsa"."WorkflowExecutionLogRecords" ("ActivityNodeId");

CREATE INDEX "IX_WorkflowExecutionLogRecord_ActivityType" ON "Elsa"."WorkflowExecutionLogRecords" ("ActivityType");

CREATE INDEX "IX_WorkflowExecutionLogRecord_ActivityType_ActivityTypeVersion" ON "Elsa"."WorkflowExecutionLogRecords" ("ActivityType", "ActivityTypeVersion");

CREATE INDEX "IX_WorkflowExecutionLogRecord_ActivityTypeVersion" ON "Elsa"."WorkflowExecutionLogRecords" ("ActivityTypeVersion");

CREATE INDEX "IX_WorkflowExecutionLogRecord_EventName" ON "Elsa"."WorkflowExecutionLogRecords" ("EventName");

CREATE INDEX "IX_WorkflowExecutionLogRecord_ParentActivityInstanceId" ON "Elsa"."WorkflowExecutionLogRecords" ("ParentActivityInstanceId");

CREATE INDEX "IX_WorkflowExecutionLogRecord_Sequence" ON "Elsa"."WorkflowExecutionLogRecords" ("Sequence");

CREATE INDEX "IX_WorkflowExecutionLogRecord_Timestamp" ON "Elsa"."WorkflowExecutionLogRecords" ("Timestamp");

CREATE INDEX "IX_WorkflowExecutionLogRecord_Timestamp_Sequence" ON "Elsa"."WorkflowExecutionLogRecords" ("Timestamp", "Sequence");

CREATE INDEX "IX_WorkflowExecutionLogRecord_WorkflowDefinitionId" ON "Elsa"."WorkflowExecutionLogRecords" ("WorkflowDefinitionId");

CREATE INDEX "IX_WorkflowExecutionLogRecord_WorkflowDefinitionVersionId" ON "Elsa"."WorkflowExecutionLogRecords" ("WorkflowDefinitionVersionId");

CREATE INDEX "IX_WorkflowExecutionLogRecord_WorkflowInstanceId" ON "Elsa"."WorkflowExecutionLogRecords" ("WorkflowInstanceId");

CREATE INDEX "IX_WorkflowExecutionLogRecord_WorkflowVersion" ON "Elsa"."WorkflowExecutionLogRecords" ("WorkflowVersion");

CREATE INDEX "IX_WorkflowInboxMessage_ActivityInstanceId" ON "Elsa"."WorkflowInboxMessages" ("ActivityInstanceId");

CREATE INDEX "IX_WorkflowInboxMessage_ActivityTypeName" ON "Elsa"."WorkflowInboxMessages" ("ActivityTypeName");

CREATE INDEX "IX_WorkflowInboxMessage_CorrelationId" ON "Elsa"."WorkflowInboxMessages" ("CorrelationId");

CREATE INDEX "IX_WorkflowInboxMessage_CreatedAt" ON "Elsa"."WorkflowInboxMessages" ("CreatedAt");

CREATE INDEX "IX_WorkflowInboxMessage_ExpiresAt" ON "Elsa"."WorkflowInboxMessages" ("ExpiresAt");

CREATE INDEX "IX_WorkflowInboxMessage_Hash" ON "Elsa"."WorkflowInboxMessages" ("Hash");

CREATE INDEX "IX_WorkflowInboxMessage_WorkflowInstanceId" ON "Elsa"."WorkflowInboxMessages" ("WorkflowInstanceId");

INSERT INTO "Elsa"."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20231024160952_Initial', '7.0.14');

COMMIT;

START TRANSACTION;

ALTER TABLE "Elsa"."ActivityExecutionRecords" ADD "SerializedActivityStateCompressionAlgorithm" text NULL;

ALTER TABLE "Elsa"."ActivityExecutionRecords" ADD "SerializedProperties" text NULL;

CREATE TABLE "Elsa"."KeyValuePairs" (
    "Key" text NOT NULL,
    "SerializedValue" text NOT NULL,
    CONSTRAINT "PK_KeyValuePairs" PRIMARY KEY ("Key")
);

INSERT INTO "Elsa"."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20240312145147_V3_1', '7.0.14');

COMMIT;

