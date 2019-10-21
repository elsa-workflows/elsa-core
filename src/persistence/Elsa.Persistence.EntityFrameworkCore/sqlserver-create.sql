IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;

GO

CREATE TABLE [WorkflowDefinitionVersions] (
    [Id] nvarchar(450) NOT NULL,
    [DefinitionId] nvarchar(max) NULL,
    [Version] int NOT NULL,
    [Name] nvarchar(max) NULL,
    [Description] nvarchar(max) NULL,
    [Activities] nvarchar(max) NULL,
    [Connections] nvarchar(max) NULL,
    [Variables] nvarchar(max) NULL,
    [IsSingleton] bit NOT NULL,
    [IsDisabled] bit NOT NULL,
    [IsPublished] bit NOT NULL,
    [IsLatest] bit NOT NULL,
    CONSTRAINT [PK_WorkflowDefinitionVersions] PRIMARY KEY ([Id])
);

GO

CREATE TABLE [WorkflowInstances] (
    [Id] nvarchar(450) NOT NULL,
    [DefinitionId] nvarchar(max) NULL,
    [Version] int NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [CorrelationId] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [StartedAt] datetime2 NULL,
    [FinishedAt] datetime2 NULL,
    [FaultedAt] datetime2 NULL,
    [AbortedAt] datetime2 NULL,
    [Activities] nvarchar(max) NULL,
    [Scopes] nvarchar(max) NULL,
    [Input] nvarchar(max) NULL,
    [BlockingActivities] nvarchar(max) NULL,
    [ExecutionLog] nvarchar(max) NULL,
    [Fault] nvarchar(max) NULL,
    CONSTRAINT [PK_WorkflowInstances] PRIMARY KEY ([Id])
);

GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20190929143746_InitialCreate', N'2.2.6-servicing-10079');

GO
