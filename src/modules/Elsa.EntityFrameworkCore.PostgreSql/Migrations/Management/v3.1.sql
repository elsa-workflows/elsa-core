
CREATE TABLE IF NOT EXISTS "Elsa"."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

CREATE TABLE "Elsa"."WorkflowDefinitions" (
    "Id" text NOT NULL,
    "DefinitionId" text NOT NULL,
    "Name" text NULL,
    "Description" text NULL,
    "ToolVersion" text NULL,
    "ProviderName" text NULL,
    "MaterializerName" text NOT NULL,
    "MaterializerContext" text NULL,
    "StringData" text NULL,
    "BinaryData" bytea NULL,
    "IsReadonly" boolean NOT NULL,
    "Data" text NULL,
    "UsableAsActivity" boolean NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "Version" integer NOT NULL,
    "IsLatest" boolean NOT NULL,
    "IsPublished" boolean NOT NULL,
    CONSTRAINT "PK_WorkflowDefinitions" PRIMARY KEY ("Id")
);

CREATE TABLE "Elsa"."WorkflowInstances" (
    "Id" text NOT NULL,
    "DefinitionId" text NOT NULL,
    "DefinitionVersionId" text NOT NULL,
    "Version" integer NOT NULL,
    "Status" text NOT NULL,
    "SubStatus" text NOT NULL,
    "CorrelationId" text NULL,
    "Name" text NULL,
    "IncidentCount" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "FinishedAt" timestamp with time zone NULL,
    "Data" text NULL,
    CONSTRAINT "PK_WorkflowInstances" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_WorkflowDefinition_DefinitionId_Version" ON "Elsa"."WorkflowDefinitions" ("DefinitionId", "Version");

CREATE INDEX "IX_WorkflowDefinition_IsLatest" ON "Elsa"."WorkflowDefinitions" ("IsLatest");

CREATE INDEX "IX_WorkflowDefinition_IsPublished" ON "Elsa"."WorkflowDefinitions" ("IsPublished");

CREATE INDEX "IX_WorkflowDefinition_Name" ON "Elsa"."WorkflowDefinitions" ("Name");

CREATE INDEX "IX_WorkflowDefinition_UsableAsActivity" ON "Elsa"."WorkflowDefinitions" ("UsableAsActivity");

CREATE INDEX "IX_WorkflowDefinition_Version" ON "Elsa"."WorkflowDefinitions" ("Version");

CREATE INDEX "IX_WorkflowInstance_CorrelationId" ON "Elsa"."WorkflowInstances" ("CorrelationId");

CREATE INDEX "IX_WorkflowInstance_CreatedAt" ON "Elsa"."WorkflowInstances" ("CreatedAt");

CREATE INDEX "IX_WorkflowInstance_DefinitionId" ON "Elsa"."WorkflowInstances" ("DefinitionId");

CREATE INDEX "IX_WorkflowInstance_FinishedAt" ON "Elsa"."WorkflowInstances" ("FinishedAt");

CREATE INDEX "IX_WorkflowInstance_Name" ON "Elsa"."WorkflowInstances" ("Name");

CREATE INDEX "IX_WorkflowInstance_Status" ON "Elsa"."WorkflowInstances" ("Status");

CREATE INDEX "IX_WorkflowInstance_Status_DefinitionId" ON "Elsa"."WorkflowInstances" ("Status", "DefinitionId");

CREATE INDEX "IX_WorkflowInstance_Status_SubStatus" ON "Elsa"."WorkflowInstances" ("Status", "SubStatus");

CREATE INDEX "IX_WorkflowInstance_Status_SubStatus_DefinitionId_Version" ON "Elsa"."WorkflowInstances" ("Status", "SubStatus", "DefinitionId", "Version");

CREATE INDEX "IX_WorkflowInstance_SubStatus" ON "Elsa"."WorkflowInstances" ("SubStatus");

CREATE INDEX "IX_WorkflowInstance_SubStatus_DefinitionId" ON "Elsa"."WorkflowInstances" ("SubStatus", "DefinitionId");

CREATE INDEX "IX_WorkflowInstance_UpdatedAt" ON "Elsa"."WorkflowInstances" ("UpdatedAt");

INSERT INTO "Elsa"."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20231015122234_Initial', '7.0.14');

COMMIT;

START TRANSACTION;

ALTER TABLE "Elsa"."WorkflowInstances" ADD "DataCompressionAlgorithm" text NULL;

ALTER TABLE "Elsa"."WorkflowInstances" ADD "IsSystem" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE "Elsa"."WorkflowDefinitions" ADD "IsSystem" boolean NOT NULL DEFAULT FALSE;

CREATE INDEX "IX_WorkflowInstance_IsSystem" ON "Elsa"."WorkflowInstances" ("IsSystem");

CREATE INDEX "IX_WorkflowDefinition_IsSystem" ON "Elsa"."WorkflowDefinitions" ("IsSystem");

INSERT INTO "Elsa"."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20240312145207_V3_1', '7.0.14');

COMMIT;

