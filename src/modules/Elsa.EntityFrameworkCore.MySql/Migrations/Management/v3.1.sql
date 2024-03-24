CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;

ALTER DATABASE CHARACTER SET utf8mb4;

CREATE TABLE `WorkflowDefinitions` (
    `Id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `DefinitionId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Name` varchar(255) CHARACTER SET utf8mb4 NULL,
    `Description` longtext CHARACTER SET utf8mb4 NULL,
    `ToolVersion` longtext CHARACTER SET utf8mb4 NULL,
    `ProviderName` longtext CHARACTER SET utf8mb4 NULL,
    `MaterializerName` longtext CHARACTER SET utf8mb4 NOT NULL,
    `MaterializerContext` longtext CHARACTER SET utf8mb4 NULL,
    `StringData` longtext CHARACTER SET utf8mb4 NULL,
    `BinaryData` longblob NULL,
    `IsReadonly` tinyint(1) NOT NULL,
    `Data` longtext CHARACTER SET utf8mb4 NULL,
    `UsableAsActivity` tinyint(1) NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `Version` int NOT NULL,
    `IsLatest` tinyint(1) NOT NULL,
    `IsPublished` tinyint(1) NOT NULL,
    CONSTRAINT `PK_WorkflowDefinitions` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `WorkflowInstances` (
    `Id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `DefinitionId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `DefinitionVersionId` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Version` int NOT NULL,
    `Status` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `SubStatus` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `CorrelationId` varchar(255) CHARACTER SET utf8mb4 NULL,
    `Name` varchar(255) CHARACTER SET utf8mb4 NULL,
    `IncidentCount` int NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    `FinishedAt` datetime(6) NULL,
    `Data` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_WorkflowInstances` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE UNIQUE INDEX `IX_WorkflowDefinition_DefinitionId_Version` ON `WorkflowDefinitions` (`DefinitionId`, `Version`);

CREATE INDEX `IX_WorkflowDefinition_IsLatest` ON `WorkflowDefinitions` (`IsLatest`);

CREATE INDEX `IX_WorkflowDefinition_IsPublished` ON `WorkflowDefinitions` (`IsPublished`);

CREATE INDEX `IX_WorkflowDefinition_Name` ON `WorkflowDefinitions` (`Name`);

CREATE INDEX `IX_WorkflowDefinition_UsableAsActivity` ON `WorkflowDefinitions` (`UsableAsActivity`);

CREATE INDEX `IX_WorkflowDefinition_Version` ON `WorkflowDefinitions` (`Version`);

CREATE INDEX `IX_WorkflowInstance_CorrelationId` ON `WorkflowInstances` (`CorrelationId`);

CREATE INDEX `IX_WorkflowInstance_CreatedAt` ON `WorkflowInstances` (`CreatedAt`);

CREATE INDEX `IX_WorkflowInstance_DefinitionId` ON `WorkflowInstances` (`DefinitionId`);

CREATE INDEX `IX_WorkflowInstance_FinishedAt` ON `WorkflowInstances` (`FinishedAt`);

CREATE INDEX `IX_WorkflowInstance_Name` ON `WorkflowInstances` (`Name`);

CREATE INDEX `IX_WorkflowInstance_Status` ON `WorkflowInstances` (`Status`);

CREATE INDEX `IX_WorkflowInstance_Status_DefinitionId` ON `WorkflowInstances` (`Status`, `DefinitionId`);

CREATE INDEX `IX_WorkflowInstance_Status_SubStatus` ON `WorkflowInstances` (`Status`, `SubStatus`);

CREATE INDEX `IX_WorkflowInstance_Status_SubStatus_DefinitionId_Version` ON `WorkflowInstances` (`Status`, `SubStatus`, `DefinitionId`, `Version`);

CREATE INDEX `IX_WorkflowInstance_SubStatus` ON `WorkflowInstances` (`SubStatus`);

CREATE INDEX `IX_WorkflowInstance_SubStatus_DefinitionId` ON `WorkflowInstances` (`SubStatus`, `DefinitionId`);

CREATE INDEX `IX_WorkflowInstance_UpdatedAt` ON `WorkflowInstances` (`UpdatedAt`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20231015122223_Initial', '7.0.14');

COMMIT;

START TRANSACTION;

ALTER TABLE `WorkflowInstances` ADD `DataCompressionAlgorithm` longtext CHARACTER SET utf8mb4 NULL;

ALTER TABLE `WorkflowInstances` ADD `IsSystem` tinyint(1) NOT NULL DEFAULT FALSE;

ALTER TABLE `WorkflowDefinitions` ADD `IsSystem` tinyint(1) NOT NULL DEFAULT FALSE;

CREATE INDEX `IX_WorkflowInstance_IsSystem` ON `WorkflowInstances` (`IsSystem`);

CREATE INDEX `IX_WorkflowDefinition_IsSystem` ON `WorkflowDefinitions` (`IsSystem`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20240312145152_V3_1', '7.0.14');

COMMIT;

