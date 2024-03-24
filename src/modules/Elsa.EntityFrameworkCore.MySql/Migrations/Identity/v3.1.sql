CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;

ALTER DATABASE CHARACTER SET utf8mb4;

CREATE TABLE `Applications` (
    `Id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ClientId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `HashedClientSecret` longtext CHARACTER SET utf8mb4 NOT NULL,
    `HashedClientSecretSalt` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Name` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `HashedApiKey` longtext CHARACTER SET utf8mb4 NOT NULL,
    `HashedApiKeySalt` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Roles` longtext CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_Applications` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Roles` (
    `Id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Name` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Permissions` longtext CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_Roles` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Users` (
    `Id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Name` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `HashedPassword` longtext CHARACTER SET utf8mb4 NOT NULL,
    `HashedPasswordSalt` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Roles` longtext CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_Users` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE UNIQUE INDEX `IX_Application_ClientId` ON `Applications` (`ClientId`);

CREATE UNIQUE INDEX `IX_Application_Name` ON `Applications` (`Name`);

CREATE UNIQUE INDEX `IX_Role_Name` ON `Roles` (`Name`);

CREATE UNIQUE INDEX `IX_User_Name` ON `Users` (`Name`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20231015122238_Initial', '7.0.14');

COMMIT;

START TRANSACTION;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20240312145212_V3_1', '7.0.14');

COMMIT;

