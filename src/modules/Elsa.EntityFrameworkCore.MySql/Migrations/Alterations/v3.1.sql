CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;

ALTER DATABASE CHARACTER SET utf8mb4;

CREATE TABLE `AlterationJobs` (
    `Id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `PlanId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `WorkflowInstanceId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Status` int NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `StartedAt` datetime(6) NULL,
    `CompletedAt` datetime(6) NULL,
    `SerializedLog` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_AlterationJobs` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `AlterationPlans` (
    `Id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Status` int NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `StartedAt` datetime(6) NULL,
    `CompletedAt` datetime(6) NULL,
    `SerializedAlterations` longtext CHARACTER SET utf8mb4 NULL,
    `SerializedWorkflowInstanceIds` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_AlterationPlans` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_AlterationJob_CompletedAt` ON `AlterationJobs` (`CompletedAt`);

CREATE INDEX `IX_AlterationJob_CreatedAt` ON `AlterationJobs` (`CreatedAt`);

CREATE INDEX `IX_AlterationJob_PlanId` ON `AlterationJobs` (`PlanId`);

CREATE INDEX `IX_AlterationJob_StartedAt` ON `AlterationJobs` (`StartedAt`);

CREATE INDEX `IX_AlterationJob_Status` ON `AlterationJobs` (`Status`);

CREATE INDEX `IX_AlterationJob_WorkflowInstanceId` ON `AlterationJobs` (`WorkflowInstanceId`);

CREATE INDEX `IX_AlterationPlan_CompletedAt` ON `AlterationPlans` (`CompletedAt`);

CREATE INDEX `IX_AlterationPlan_CreatedAt` ON `AlterationPlans` (`CreatedAt`);

CREATE INDEX `IX_AlterationPlan_StartedAt` ON `AlterationPlans` (`StartedAt`);

CREATE INDEX `IX_AlterationPlan_Status` ON `AlterationPlans` (`Status`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20231015122151_Initial', '7.0.14');

COMMIT;

START TRANSACTION;

ALTER TABLE `AlterationPlans` CHANGE `SerializedWorkflowInstanceIds` `SerializedWorkflowInstanceFilter` longtext NULL;

ALTER TABLE `AlterationPlans` MODIFY COLUMN `Status` varchar(255) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `AlterationJobs` MODIFY COLUMN `Status` varchar(255) CHARACTER SET utf8mb4 NOT NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20240312145108_V3_1', '7.0.14');

COMMIT;

