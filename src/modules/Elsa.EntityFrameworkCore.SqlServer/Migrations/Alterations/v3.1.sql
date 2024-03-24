IF OBJECT_ID(N'[Elsa].[__EFMigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'Elsa') IS NULL EXEC(N'CREATE SCHEMA [Elsa];');
    CREATE TABLE [Elsa].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF SCHEMA_ID(N'Elsa') IS NULL EXEC(N'CREATE SCHEMA [Elsa];');
GO

CREATE TABLE [Elsa].[AlterationJobs] (
    [Id] nvarchar(450) NOT NULL,
    [PlanId] nvarchar(450) NOT NULL,
    [WorkflowInstanceId] nvarchar(450) NOT NULL,
    [Status] int NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [StartedAt] datetimeoffset NULL,
    [CompletedAt] datetimeoffset NULL,
    [SerializedLog] nvarchar(max) NULL,
    CONSTRAINT [PK_AlterationJobs] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Elsa].[AlterationPlans] (
    [Id] nvarchar(450) NOT NULL,
    [Status] int NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [StartedAt] datetimeoffset NULL,
    [CompletedAt] datetimeoffset NULL,
    [SerializedAlterations] nvarchar(max) NULL,
    [SerializedWorkflowInstanceIds] nvarchar(max) NULL,
    CONSTRAINT [PK_AlterationPlans] PRIMARY KEY ([Id])
);
GO

CREATE INDEX [IX_AlterationJob_CompletedAt] ON [Elsa].[AlterationJobs] ([CompletedAt]);
GO

CREATE INDEX [IX_AlterationJob_CreatedAt] ON [Elsa].[AlterationJobs] ([CreatedAt]);
GO

CREATE INDEX [IX_AlterationJob_PlanId] ON [Elsa].[AlterationJobs] ([PlanId]);
GO

CREATE INDEX [IX_AlterationJob_StartedAt] ON [Elsa].[AlterationJobs] ([StartedAt]);
GO

CREATE INDEX [IX_AlterationJob_Status] ON [Elsa].[AlterationJobs] ([Status]);
GO

CREATE INDEX [IX_AlterationJob_WorkflowInstanceId] ON [Elsa].[AlterationJobs] ([WorkflowInstanceId]);
GO

CREATE INDEX [IX_AlterationPlan_CompletedAt] ON [Elsa].[AlterationPlans] ([CompletedAt]);
GO

CREATE INDEX [IX_AlterationPlan_CreatedAt] ON [Elsa].[AlterationPlans] ([CreatedAt]);
GO

CREATE INDEX [IX_AlterationPlan_StartedAt] ON [Elsa].[AlterationPlans] ([StartedAt]);
GO

CREATE INDEX [IX_AlterationPlan_Status] ON [Elsa].[AlterationPlans] ([Status]);
GO

INSERT INTO [Elsa].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20231015122155_Initial', N'7.0.14');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

EXEC sp_rename N'[Elsa].[AlterationPlans].[SerializedWorkflowInstanceIds]', N'SerializedWorkflowInstanceFilter', N'COLUMN';
GO

DROP INDEX [IX_AlterationPlan_Status] ON [Elsa].[AlterationPlans];
DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Elsa].[AlterationPlans]') AND [c].[name] = N'Status');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Elsa].[AlterationPlans] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [Elsa].[AlterationPlans] ALTER COLUMN [Status] nvarchar(450) NOT NULL;
CREATE INDEX [IX_AlterationPlan_Status] ON [Elsa].[AlterationPlans] ([Status]);
GO

DROP INDEX [IX_AlterationJob_Status] ON [Elsa].[AlterationJobs];
DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Elsa].[AlterationJobs]') AND [c].[name] = N'Status');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Elsa].[AlterationJobs] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [Elsa].[AlterationJobs] ALTER COLUMN [Status] nvarchar(450) NOT NULL;
CREATE INDEX [IX_AlterationJob_Status] ON [Elsa].[AlterationJobs] ([Status]);
GO

INSERT INTO [Elsa].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20240312145115_V3_1', N'7.0.14');
GO

COMMIT;
GO

