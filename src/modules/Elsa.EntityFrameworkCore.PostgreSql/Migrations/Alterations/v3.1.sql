
CREATE TABLE IF NOT EXISTS "Elsa"."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

CREATE TABLE "Elsa"."AlterationJobs" (
    "Id" text NOT NULL,
    "PlanId" text NOT NULL,
    "WorkflowInstanceId" text NOT NULL,
    "Status" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "StartedAt" timestamp with time zone NULL,
    "CompletedAt" timestamp with time zone NULL,
    "SerializedLog" text NULL,
    CONSTRAINT "PK_AlterationJobs" PRIMARY KEY ("Id")
);

CREATE TABLE "Elsa"."AlterationPlans" (
    "Id" text NOT NULL,
    "Status" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "StartedAt" timestamp with time zone NULL,
    "CompletedAt" timestamp with time zone NULL,
    "SerializedAlterations" text NULL,
    "SerializedWorkflowInstanceIds" text NULL,
    CONSTRAINT "PK_AlterationPlans" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_AlterationJob_CompletedAt" ON "Elsa"."AlterationJobs" ("CompletedAt");

CREATE INDEX "IX_AlterationJob_CreatedAt" ON "Elsa"."AlterationJobs" ("CreatedAt");

CREATE INDEX "IX_AlterationJob_PlanId" ON "Elsa"."AlterationJobs" ("PlanId");

CREATE INDEX "IX_AlterationJob_StartedAt" ON "Elsa"."AlterationJobs" ("StartedAt");

CREATE INDEX "IX_AlterationJob_Status" ON "Elsa"."AlterationJobs" ("Status");

CREATE INDEX "IX_AlterationJob_WorkflowInstanceId" ON "Elsa"."AlterationJobs" ("WorkflowInstanceId");

CREATE INDEX "IX_AlterationPlan_CompletedAt" ON "Elsa"."AlterationPlans" ("CompletedAt");

CREATE INDEX "IX_AlterationPlan_CreatedAt" ON "Elsa"."AlterationPlans" ("CreatedAt");

CREATE INDEX "IX_AlterationPlan_StartedAt" ON "Elsa"."AlterationPlans" ("StartedAt");

CREATE INDEX "IX_AlterationPlan_Status" ON "Elsa"."AlterationPlans" ("Status");

INSERT INTO "Elsa"."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20231015122203_Initial', '7.0.14');

COMMIT;

START TRANSACTION;

ALTER TABLE "Elsa"."AlterationPlans" RENAME COLUMN "SerializedWorkflowInstanceIds" TO "SerializedWorkflowInstanceFilter";

ALTER TABLE "Elsa"."AlterationPlans" ALTER COLUMN "Status" TYPE text;

ALTER TABLE "Elsa"."AlterationJobs" ALTER COLUMN "Status" TYPE text;

INSERT INTO "Elsa"."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20240312145127_V3_1', '7.0.14');

COMMIT;

