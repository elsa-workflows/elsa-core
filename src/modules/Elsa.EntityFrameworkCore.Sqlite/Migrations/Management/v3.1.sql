CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;

CREATE TABLE "WorkflowDefinitions" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_WorkflowDefinitions" PRIMARY KEY,
    "DefinitionId" TEXT NOT NULL,
    "Name" TEXT NULL,
    "Description" TEXT NULL,
    "ToolVersion" TEXT NULL,
    "ProviderName" TEXT NULL,
    "MaterializerName" TEXT NOT NULL,
    "MaterializerContext" TEXT NULL,
    "StringData" TEXT NULL,
    "BinaryData" BLOB NULL,
    "IsReadonly" INTEGER NOT NULL,
    "Data" TEXT NULL,
    "UsableAsActivity" INTEGER NULL,
    "CreatedAt" TEXT NOT NULL,
    "Version" INTEGER NOT NULL,
    "IsLatest" INTEGER NOT NULL,
    "IsPublished" INTEGER NOT NULL
);

CREATE TABLE "WorkflowInstances" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_WorkflowInstances" PRIMARY KEY,
    "DefinitionId" TEXT NOT NULL,
    "DefinitionVersionId" TEXT NOT NULL,
    "Version" INTEGER NOT NULL,
    "Status" TEXT NOT NULL,
    "SubStatus" TEXT NOT NULL,
    "CorrelationId" TEXT NULL,
    "Name" TEXT NULL,
    "IncidentCount" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    "FinishedAt" TEXT NULL,
    "Data" TEXT NULL
);

CREATE UNIQUE INDEX "IX_WorkflowDefinition_DefinitionId_Version" ON "WorkflowDefinitions" ("DefinitionId", "Version");

CREATE INDEX "IX_WorkflowDefinition_IsLatest" ON "WorkflowDefinitions" ("IsLatest");

CREATE INDEX "IX_WorkflowDefinition_IsPublished" ON "WorkflowDefinitions" ("IsPublished");

CREATE INDEX "IX_WorkflowDefinition_Name" ON "WorkflowDefinitions" ("Name");

CREATE INDEX "IX_WorkflowDefinition_UsableAsActivity" ON "WorkflowDefinitions" ("UsableAsActivity");

CREATE INDEX "IX_WorkflowDefinition_Version" ON "WorkflowDefinitions" ("Version");

CREATE INDEX "IX_WorkflowInstance_CorrelationId" ON "WorkflowInstances" ("CorrelationId");

CREATE INDEX "IX_WorkflowInstance_CreatedAt" ON "WorkflowInstances" ("CreatedAt");

CREATE INDEX "IX_WorkflowInstance_DefinitionId" ON "WorkflowInstances" ("DefinitionId");

CREATE INDEX "IX_WorkflowInstance_FinishedAt" ON "WorkflowInstances" ("FinishedAt");

CREATE INDEX "IX_WorkflowInstance_Name" ON "WorkflowInstances" ("Name");

CREATE INDEX "IX_WorkflowInstance_Status" ON "WorkflowInstances" ("Status");

CREATE INDEX "IX_WorkflowInstance_Status_DefinitionId" ON "WorkflowInstances" ("Status", "DefinitionId");

CREATE INDEX "IX_WorkflowInstance_Status_SubStatus" ON "WorkflowInstances" ("Status", "SubStatus");

CREATE INDEX "IX_WorkflowInstance_Status_SubStatus_DefinitionId_Version" ON "WorkflowInstances" ("Status", "SubStatus", "DefinitionId", "Version");

CREATE INDEX "IX_WorkflowInstance_SubStatus" ON "WorkflowInstances" ("SubStatus");

CREATE INDEX "IX_WorkflowInstance_SubStatus_DefinitionId" ON "WorkflowInstances" ("SubStatus", "DefinitionId");

CREATE INDEX "IX_WorkflowInstance_UpdatedAt" ON "WorkflowInstances" ("UpdatedAt");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20231015122231_Initial', '7.0.14');

COMMIT;

BEGIN TRANSACTION;

ALTER TABLE "WorkflowInstances" ADD "DataCompressionAlgorithm" TEXT NULL;

ALTER TABLE "WorkflowInstances" ADD "IsSystem" INTEGER NOT NULL DEFAULT 0;

ALTER TABLE "WorkflowDefinitions" ADD "IsSystem" INTEGER NOT NULL DEFAULT 0;

CREATE INDEX "IX_WorkflowInstance_IsSystem" ON "WorkflowInstances" ("IsSystem");

CREATE INDEX "IX_WorkflowDefinition_IsSystem" ON "WorkflowDefinitions" ("IsSystem");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20240312145202_V3_1', '7.0.14');

COMMIT;

