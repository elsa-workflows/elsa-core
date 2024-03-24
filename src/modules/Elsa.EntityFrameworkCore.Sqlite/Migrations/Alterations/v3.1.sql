CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;

CREATE TABLE "AlterationJobs" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_AlterationJobs" PRIMARY KEY,
    "PlanId" TEXT NOT NULL,
    "WorkflowInstanceId" TEXT NOT NULL,
    "Status" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "StartedAt" TEXT NULL,
    "CompletedAt" TEXT NULL,
    "SerializedLog" TEXT NULL
);

CREATE TABLE "AlterationPlans" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_AlterationPlans" PRIMARY KEY,
    "Status" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "StartedAt" TEXT NULL,
    "CompletedAt" TEXT NULL,
    "SerializedAlterations" TEXT NULL,
    "SerializedWorkflowInstanceIds" TEXT NULL
);

CREATE INDEX "IX_AlterationJob_CompletedAt" ON "AlterationJobs" ("CompletedAt");

CREATE INDEX "IX_AlterationJob_CreatedAt" ON "AlterationJobs" ("CreatedAt");

CREATE INDEX "IX_AlterationJob_PlanId" ON "AlterationJobs" ("PlanId");

CREATE INDEX "IX_AlterationJob_StartedAt" ON "AlterationJobs" ("StartedAt");

CREATE INDEX "IX_AlterationJob_Status" ON "AlterationJobs" ("Status");

CREATE INDEX "IX_AlterationJob_WorkflowInstanceId" ON "AlterationJobs" ("WorkflowInstanceId");

CREATE INDEX "IX_AlterationPlan_CompletedAt" ON "AlterationPlans" ("CompletedAt");

CREATE INDEX "IX_AlterationPlan_CreatedAt" ON "AlterationPlans" ("CreatedAt");

CREATE INDEX "IX_AlterationPlan_StartedAt" ON "AlterationPlans" ("StartedAt");

CREATE INDEX "IX_AlterationPlan_Status" ON "AlterationPlans" ("Status");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20231015122159_Initial', '7.0.14');

COMMIT;

BEGIN TRANSACTION;

ALTER TABLE "AlterationPlans" RENAME COLUMN "SerializedWorkflowInstanceIds" TO "SerializedWorkflowInstanceFilter";

CREATE TABLE "ef_temp_AlterationPlans" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_AlterationPlans" PRIMARY KEY,
    "CompletedAt" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    "SerializedAlterations" TEXT NULL,
    "SerializedWorkflowInstanceFilter" TEXT NULL,
    "StartedAt" TEXT NULL,
    "Status" TEXT NOT NULL
);

INSERT INTO "ef_temp_AlterationPlans" ("Id", "CompletedAt", "CreatedAt", "SerializedAlterations", "SerializedWorkflowInstanceFilter", "StartedAt", "Status")
SELECT "Id", "CompletedAt", "CreatedAt", "SerializedAlterations", "SerializedWorkflowInstanceFilter", "StartedAt", "Status"
FROM "AlterationPlans";

CREATE TABLE "ef_temp_AlterationJobs" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_AlterationJobs" PRIMARY KEY,
    "CompletedAt" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    "PlanId" TEXT NOT NULL,
    "SerializedLog" TEXT NULL,
    "StartedAt" TEXT NULL,
    "Status" TEXT NOT NULL,
    "WorkflowInstanceId" TEXT NOT NULL
);

INSERT INTO "ef_temp_AlterationJobs" ("Id", "CompletedAt", "CreatedAt", "PlanId", "SerializedLog", "StartedAt", "Status", "WorkflowInstanceId")
SELECT "Id", "CompletedAt", "CreatedAt", "PlanId", "SerializedLog", "StartedAt", "Status", "WorkflowInstanceId"
FROM "AlterationJobs";

COMMIT;

PRAGMA foreign_keys = 0;

BEGIN TRANSACTION;

DROP TABLE "AlterationPlans";

ALTER TABLE "ef_temp_AlterationPlans" RENAME TO "AlterationPlans";

DROP TABLE "AlterationJobs";

ALTER TABLE "ef_temp_AlterationJobs" RENAME TO "AlterationJobs";

COMMIT;

PRAGMA foreign_keys = 1;

BEGIN TRANSACTION;

CREATE INDEX "IX_AlterationPlan_CompletedAt" ON "AlterationPlans" ("CompletedAt");

CREATE INDEX "IX_AlterationPlan_CreatedAt" ON "AlterationPlans" ("CreatedAt");

CREATE INDEX "IX_AlterationPlan_StartedAt" ON "AlterationPlans" ("StartedAt");

CREATE INDEX "IX_AlterationPlan_Status" ON "AlterationPlans" ("Status");

CREATE INDEX "IX_AlterationJob_CompletedAt" ON "AlterationJobs" ("CompletedAt");

CREATE INDEX "IX_AlterationJob_CreatedAt" ON "AlterationJobs" ("CreatedAt");

CREATE INDEX "IX_AlterationJob_PlanId" ON "AlterationJobs" ("PlanId");

CREATE INDEX "IX_AlterationJob_StartedAt" ON "AlterationJobs" ("StartedAt");

CREATE INDEX "IX_AlterationJob_Status" ON "AlterationJobs" ("Status");

CREATE INDEX "IX_AlterationJob_WorkflowInstanceId" ON "AlterationJobs" ("WorkflowInstanceId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20240312145121_V3_1', '7.0.14');

COMMIT;

