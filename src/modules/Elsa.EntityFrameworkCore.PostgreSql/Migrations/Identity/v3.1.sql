
CREATE TABLE IF NOT EXISTS "Elsa"."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

CREATE TABLE "Elsa"."Applications" (
    "Id" text NOT NULL,
    "ClientId" text NOT NULL,
    "HashedClientSecret" text NOT NULL,
    "HashedClientSecretSalt" text NOT NULL,
    "Name" text NOT NULL,
    "HashedApiKey" text NOT NULL,
    "HashedApiKeySalt" text NOT NULL,
    "Roles" text NOT NULL,
    CONSTRAINT "PK_Applications" PRIMARY KEY ("Id")
);

COMMIT;

CREATE TABLE "Elsa"."Roles" (
    "Id" text NOT NULL,
    "Name" text NOT NULL,
    "Permissions" text NOT NULL,
    CONSTRAINT "PK_Roles" PRIMARY KEY ("Id")
);

CREATE TABLE "Elsa"."Users" (
    "Id" text NOT NULL,
    "Name" text NOT NULL,
    "HashedPassword" text NOT NULL,
    "HashedPasswordSalt" text NOT NULL,
    "Roles" text NOT NULL,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_Application_ClientId" ON "Elsa"."Applications" ("ClientId");

CREATE UNIQUE INDEX "IX_Application_Name" ON "Elsa"."Applications" ("Name");

CREATE UNIQUE INDEX "IX_Role_Name" ON "Elsa"."Roles" ("Name");

CREATE UNIQUE INDEX "IX_User_Name" ON "Elsa"."Users" ("Name");

INSERT INTO "Elsa"."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20231015122250_Initial', '7.0.14');

COMMIT;

START TRANSACTION;

INSERT INTO "Elsa"."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20240312145226_V3_1', '7.0.14');

COMMIT;

