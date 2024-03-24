CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;

CREATE TABLE "Applications" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Applications" PRIMARY KEY,
    "ClientId" TEXT NOT NULL,
    "HashedClientSecret" TEXT NOT NULL,
    "HashedClientSecretSalt" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "HashedApiKey" TEXT NOT NULL,
    "HashedApiKeySalt" TEXT NOT NULL,
    "Roles" TEXT NOT NULL
);

CREATE TABLE "Roles" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Roles" PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "Permissions" TEXT NOT NULL
);

CREATE TABLE "Users" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "HashedPassword" TEXT NOT NULL,
    "HashedPasswordSalt" TEXT NOT NULL,
    "Roles" TEXT NOT NULL
);

CREATE UNIQUE INDEX "IX_Application_ClientId" ON "Applications" ("ClientId");

CREATE UNIQUE INDEX "IX_Application_Name" ON "Applications" ("Name");

CREATE UNIQUE INDEX "IX_Role_Name" ON "Roles" ("Name");

CREATE UNIQUE INDEX "IX_User_Name" ON "Users" ("Name");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20231015122246_Initial', '7.0.14');

COMMIT;

BEGIN TRANSACTION;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20240312145221_V3_1', '7.0.14');

COMMIT;

