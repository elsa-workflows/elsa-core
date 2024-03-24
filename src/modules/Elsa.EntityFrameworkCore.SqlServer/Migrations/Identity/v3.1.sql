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

CREATE TABLE [Elsa].[Applications] (
    [Id] nvarchar(450) NOT NULL,
    [ClientId] nvarchar(450) NOT NULL,
    [HashedClientSecret] nvarchar(max) NOT NULL,
    [HashedClientSecretSalt] nvarchar(max) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [HashedApiKey] nvarchar(max) NOT NULL,
    [HashedApiKeySalt] nvarchar(max) NOT NULL,
    [Roles] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Applications] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Elsa].[Roles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Permissions] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Elsa].[Users] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [HashedPassword] nvarchar(max) NOT NULL,
    [HashedPasswordSalt] nvarchar(max) NOT NULL,
    [Roles] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);
GO

CREATE UNIQUE INDEX [IX_Application_ClientId] ON [Elsa].[Applications] ([ClientId]);
GO

CREATE UNIQUE INDEX [IX_Application_Name] ON [Elsa].[Applications] ([Name]);
GO

CREATE UNIQUE INDEX [IX_Role_Name] ON [Elsa].[Roles] ([Name]);
GO

CREATE UNIQUE INDEX [IX_User_Name] ON [Elsa].[Users] ([Name]);
GO

INSERT INTO [Elsa].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20231015122242_Initial', N'7.0.14');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [Elsa].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20240312145217_V3_1', N'7.0.14');
GO

COMMIT;
GO

