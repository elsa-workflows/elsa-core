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

CREATE TABLE [Elsa].[WorkflowDefinitions] (
    [Id] nvarchar(450) NOT NULL,
    [DefinitionId] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NULL,
    [Description] nvarchar(max) NULL,
    [ToolVersion] nvarchar(max) NULL,
    [ProviderName] nvarchar(max) NULL,
    [MaterializerName] nvarchar(max) NOT NULL,
    [MaterializerContext] nvarchar(max) NULL,
    [StringData] nvarchar(max) NULL,
    [BinaryData] varbinary(max) NULL,
    [IsReadonly] bit NOT NULL,
    [Data] nvarchar(max) NULL,
    [UsableAsActivity] bit NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [Version] int NOT NULL,
    [IsLatest] bit NOT NULL,
    [IsPublished] bit NOT NULL,
    CONSTRAINT [PK_WorkflowDefinitions] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Elsa].[WorkflowInstances] (
    [Id] nvarchar(450) NOT NULL,
    [DefinitionId] nvarchar(450) NOT NULL,
    [DefinitionVersionId] nvarchar(max) NOT NULL,
    [Version] int NOT NULL,
    [Status] nvarchar(450) NOT NULL,
    [SubStatus] nvarchar(450) NOT NULL,
    [CorrelationId] nvarchar(450) NULL,
    [Name] nvarchar(450) NULL,
    [IncidentCount] int NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NOT NULL,
    [FinishedAt] datetimeoffset NULL,
    [Data] nvarchar(max) NULL,
    CONSTRAINT [PK_WorkflowInstances] PRIMARY KEY ([Id])
);
GO

CREATE UNIQUE INDEX [IX_WorkflowDefinition_DefinitionId_Version] ON [Elsa].[WorkflowDefinitions] ([DefinitionId], [Version]);
GO

CREATE INDEX [IX_WorkflowDefinition_IsLatest] ON [Elsa].[WorkflowDefinitions] ([IsLatest]);
GO

CREATE INDEX [IX_WorkflowDefinition_IsPublished] ON [Elsa].[WorkflowDefinitions] ([IsPublished]);
GO

CREATE INDEX [IX_WorkflowDefinition_Name] ON [Elsa].[WorkflowDefinitions] ([Name]);
GO

CREATE INDEX [IX_WorkflowDefinition_UsableAsActivity] ON [Elsa].[WorkflowDefinitions] ([UsableAsActivity]);
GO

CREATE INDEX [IX_WorkflowDefinition_Version] ON [Elsa].[WorkflowDefinitions] ([Version]);
GO

CREATE INDEX [IX_WorkflowInstance_CorrelationId] ON [Elsa].[WorkflowInstances] ([CorrelationId]);
GO

CREATE INDEX [IX_WorkflowInstance_CreatedAt] ON [Elsa].[WorkflowInstances] ([CreatedAt]);
GO

CREATE INDEX [IX_WorkflowInstance_DefinitionId] ON [Elsa].[WorkflowInstances] ([DefinitionId]);
GO

CREATE INDEX [IX_WorkflowInstance_FinishedAt] ON [Elsa].[WorkflowInstances] ([FinishedAt]);
GO

CREATE INDEX [IX_WorkflowInstance_Name] ON [Elsa].[WorkflowInstances] ([Name]);
GO

CREATE INDEX [IX_WorkflowInstance_Status] ON [Elsa].[WorkflowInstances] ([Status]);
GO

CREATE INDEX [IX_WorkflowInstance_Status_DefinitionId] ON [Elsa].[WorkflowInstances] ([Status], [DefinitionId]);
GO

CREATE INDEX [IX_WorkflowInstance_Status_SubStatus] ON [Elsa].[WorkflowInstances] ([Status], [SubStatus]);
GO

CREATE INDEX [IX_WorkflowInstance_Status_SubStatus_DefinitionId_Version] ON [Elsa].[WorkflowInstances] ([Status], [SubStatus], [DefinitionId], [Version]);
GO

CREATE INDEX [IX_WorkflowInstance_SubStatus] ON [Elsa].[WorkflowInstances] ([SubStatus]);
GO

CREATE INDEX [IX_WorkflowInstance_SubStatus_DefinitionId] ON [Elsa].[WorkflowInstances] ([SubStatus], [DefinitionId]);
GO

CREATE INDEX [IX_WorkflowInstance_UpdatedAt] ON [Elsa].[WorkflowInstances] ([UpdatedAt]);
GO

INSERT INTO [Elsa].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20231015122227_Initial', N'7.0.14');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Elsa].[WorkflowInstances] ADD [DataCompressionAlgorithm] nvarchar(max) NULL;
GO

ALTER TABLE [Elsa].[WorkflowInstances] ADD [IsSystem] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [Elsa].[WorkflowDefinitions] ADD [IsSystem] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

CREATE INDEX [IX_WorkflowInstance_IsSystem] ON [Elsa].[WorkflowInstances] ([IsSystem]);
GO

CREATE INDEX [IX_WorkflowDefinition_IsSystem] ON [Elsa].[WorkflowDefinitions] ([IsSystem]);
GO

INSERT INTO [Elsa].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20240312145157_V3_1', N'7.0.14');
GO

COMMIT;
GO

