CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;

ALTER DATABASE CHARACTER SET utf8mb4;

CREATE TABLE `ActivityExecutionRecords` (
    `Id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `WorkflowInstanceId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ActivityId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ActivityNodeId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ActivityType` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ActivityTypeVersion` int NOT NULL,
    `ActivityName` varchar(255) CHARACTER SET utf8mb4 NULL,
    `StartedAt` datetime(6) NOT NULL,
    `HasBookmarks` tinyint(1) NOT NULL,
    `Status` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `CompletedAt` datetime(6) NULL,
    `SerializedActivityState` longtext CHARACTER SET utf8mb4 NULL,
    `SerializedException` longtext CHARACTER SET utf8mb4 NULL,
    `SerializedOutputs` longtext CHARACTER SET utf8mb4 NULL,
    `SerializedPayload` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_ActivityExecutionRecords` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Bookmarks` (
    `BookmarkId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ActivityTypeName` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Hash` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `WorkflowInstanceId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ActivityInstanceId` varchar(255) CHARACTER SET utf8mb4 NULL,
    `CorrelationId` longtext CHARACTER SET utf8mb4 NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `SerializedMetadata` longtext CHARACTER SET utf8mb4 NULL,
    `SerializedPayload` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_Bookmarks` PRIMARY KEY (`BookmarkId`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Triggers` (
    `Id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `WorkflowDefinitionId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `WorkflowDefinitionVersionId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Name` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ActivityId` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Hash` varchar(255) CHARACTER SET utf8mb4 NULL,
    `SerializedPayload` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_Triggers` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `WorkflowExecutionLogRecords` (
    `Id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `WorkflowDefinitionId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `WorkflowDefinitionVersionId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `WorkflowInstanceId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `WorkflowVersion` int NOT NULL,
    `ActivityInstanceId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ParentActivityInstanceId` varchar(255) CHARACTER SET utf8mb4 NULL,
    `ActivityId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ActivityType` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ActivityTypeVersion` int NOT NULL,
    `ActivityName` varchar(255) CHARACTER SET utf8mb4 NULL,
    `ActivityNodeId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Timestamp` datetime(6) NOT NULL,
    `Sequence` bigint NOT NULL,
    `EventName` varchar(255) CHARACTER SET utf8mb4 NULL,
    `Message` longtext CHARACTER SET utf8mb4 NULL,
    `Source` longtext CHARACTER SET utf8mb4 NULL,
    `SerializedActivityState` longtext CHARACTER SET utf8mb4 NULL,
    `SerializedPayload` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_WorkflowExecutionLogRecords` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `WorkflowInboxMessages` (
    `Id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ActivityTypeName` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Hash` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `WorkflowInstanceId` varchar(255) CHARACTER SET utf8mb4 NULL,
    `CorrelationId` varchar(255) CHARACTER SET utf8mb4 NULL,
    `ActivityInstanceId` varchar(255) CHARACTER SET utf8mb4 NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `ExpiresAt` datetime(6) NOT NULL,
    `SerializedBookmarkPayload` longtext CHARACTER SET utf8mb4 NULL,
    `SerializedInput` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_WorkflowInboxMessages` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_ActivityExecutionRecord_ActivityId` ON `ActivityExecutionRecords` (`ActivityId`);

CREATE INDEX `IX_ActivityExecutionRecord_ActivityName` ON `ActivityExecutionRecords` (`ActivityName`);

CREATE INDEX `IX_ActivityExecutionRecord_ActivityNodeId` ON `ActivityExecutionRecords` (`ActivityNodeId`);

CREATE INDEX `IX_ActivityExecutionRecord_ActivityType` ON `ActivityExecutionRecords` (`ActivityType`);

CREATE INDEX `IX_ActivityExecutionRecord_ActivityType_ActivityTypeVersion` ON `ActivityExecutionRecords` (`ActivityType`, `ActivityTypeVersion`);

CREATE INDEX `IX_ActivityExecutionRecord_ActivityTypeVersion` ON `ActivityExecutionRecords` (`ActivityTypeVersion`);

CREATE INDEX `IX_ActivityExecutionRecord_CompletedAt` ON `ActivityExecutionRecords` (`CompletedAt`);

CREATE INDEX `IX_ActivityExecutionRecord_HasBookmarks` ON `ActivityExecutionRecords` (`HasBookmarks`);

CREATE INDEX `IX_ActivityExecutionRecord_StartedAt` ON `ActivityExecutionRecords` (`StartedAt`);

CREATE INDEX `IX_ActivityExecutionRecord_Status` ON `ActivityExecutionRecords` (`Status`);

CREATE INDEX `IX_ActivityExecutionRecord_WorkflowInstanceId` ON `ActivityExecutionRecords` (`WorkflowInstanceId`);

CREATE INDEX `IX_StoredBookmark_ActivityInstanceId` ON `Bookmarks` (`ActivityInstanceId`);

CREATE INDEX `IX_StoredBookmark_ActivityTypeName` ON `Bookmarks` (`ActivityTypeName`);

CREATE INDEX `IX_StoredBookmark_ActivityTypeName_Hash` ON `Bookmarks` (`ActivityTypeName`, `Hash`);

CREATE INDEX `IX_StoredBookmark_ActivityTypeName_Hash_WorkflowInstanceId` ON `Bookmarks` (`ActivityTypeName`, `Hash`, `WorkflowInstanceId`);

CREATE INDEX `IX_StoredBookmark_CreatedAt` ON `Bookmarks` (`CreatedAt`);

CREATE INDEX `IX_StoredBookmark_Hash` ON `Bookmarks` (`Hash`);

CREATE INDEX `IX_StoredBookmark_WorkflowInstanceId` ON `Bookmarks` (`WorkflowInstanceId`);

CREATE INDEX `IX_StoredTrigger_Hash` ON `Triggers` (`Hash`);

CREATE INDEX `IX_StoredTrigger_Name` ON `Triggers` (`Name`);

CREATE INDEX `IX_StoredTrigger_WorkflowDefinitionId` ON `Triggers` (`WorkflowDefinitionId`);

CREATE INDEX `IX_StoredTrigger_WorkflowDefinitionVersionId` ON `Triggers` (`WorkflowDefinitionVersionId`);

CREATE INDEX `IX_WorkflowExecutionLogRecord_ActivityId` ON `WorkflowExecutionLogRecords` (`ActivityId`);

CREATE INDEX `IX_WorkflowExecutionLogRecord_ActivityInstanceId` ON `WorkflowExecutionLogRecords` (`ActivityInstanceId`);

CREATE INDEX `IX_WorkflowExecutionLogRecord_ActivityName` ON `WorkflowExecutionLogRecords` (`ActivityName`);

CREATE INDEX `IX_WorkflowExecutionLogRecord_ActivityNodeId` ON `WorkflowExecutionLogRecords` (`ActivityNodeId`);

CREATE INDEX `IX_WorkflowExecutionLogRecord_ActivityType` ON `WorkflowExecutionLogRecords` (`ActivityType`);

CREATE INDEX `IX_WorkflowExecutionLogRecord_ActivityType_ActivityTypeVersion` ON `WorkflowExecutionLogRecords` (`ActivityType`, `ActivityTypeVersion`);

CREATE INDEX `IX_WorkflowExecutionLogRecord_ActivityTypeVersion` ON `WorkflowExecutionLogRecords` (`ActivityTypeVersion`);

CREATE INDEX `IX_WorkflowExecutionLogRecord_EventName` ON `WorkflowExecutionLogRecords` (`EventName`);

CREATE INDEX `IX_WorkflowExecutionLogRecord_ParentActivityInstanceId` ON `WorkflowExecutionLogRecords` (`ParentActivityInstanceId`);

CREATE INDEX `IX_WorkflowExecutionLogRecord_Sequence` ON `WorkflowExecutionLogRecords` (`Sequence`);

CREATE INDEX `IX_WorkflowExecutionLogRecord_Timestamp` ON `WorkflowExecutionLogRecords` (`Timestamp`);

CREATE INDEX `IX_WorkflowExecutionLogRecord_Timestamp_Sequence` ON `WorkflowExecutionLogRecords` (`Timestamp`, `Sequence`);

CREATE INDEX `IX_WorkflowExecutionLogRecord_WorkflowDefinitionId` ON `WorkflowExecutionLogRecords` (`WorkflowDefinitionId`);

CREATE INDEX `IX_WorkflowExecutionLogRecord_WorkflowDefinitionVersionId` ON `WorkflowExecutionLogRecords` (`WorkflowDefinitionVersionId`);

CREATE INDEX `IX_WorkflowExecutionLogRecord_WorkflowInstanceId` ON `WorkflowExecutionLogRecords` (`WorkflowInstanceId`);

CREATE INDEX `IX_WorkflowExecutionLogRecord_WorkflowVersion` ON `WorkflowExecutionLogRecords` (`WorkflowVersion`);

CREATE INDEX `IX_WorkflowInboxMessage_ActivityInstanceId` ON `WorkflowInboxMessages` (`ActivityInstanceId`);

CREATE INDEX `IX_WorkflowInboxMessage_ActivityTypeName` ON `WorkflowInboxMessages` (`ActivityTypeName`);

CREATE INDEX `IX_WorkflowInboxMessage_CorrelationId` ON `WorkflowInboxMessages` (`CorrelationId`);

CREATE INDEX `IX_WorkflowInboxMessage_CreatedAt` ON `WorkflowInboxMessages` (`CreatedAt`);

CREATE INDEX `IX_WorkflowInboxMessage_ExpiresAt` ON `WorkflowInboxMessages` (`ExpiresAt`);

CREATE INDEX `IX_WorkflowInboxMessage_Hash` ON `WorkflowInboxMessages` (`Hash`);

CREATE INDEX `IX_WorkflowInboxMessage_WorkflowInstanceId` ON `WorkflowInboxMessages` (`WorkflowInstanceId`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20231024160940_Initial', '7.0.14');

COMMIT;

START TRANSACTION;

ALTER TABLE `ActivityExecutionRecords` ADD `SerializedActivityStateCompressionAlgorithm` longtext CHARACTER SET utf8mb4 NULL;

ALTER TABLE `ActivityExecutionRecords` ADD `SerializedProperties` longtext CHARACTER SET utf8mb4 NULL;

CREATE TABLE `KeyValuePairs` (
    `Key` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `SerializedValue` longtext CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_KeyValuePairs` PRIMARY KEY (`Key`)
) CHARACTER SET=utf8mb4;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20240312145132_V3_1', '7.0.14');

COMMIT;

