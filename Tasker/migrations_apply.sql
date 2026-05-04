IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413174002_init-mig'
)
BEGIN
    CREATE TABLE [Organizations] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Organizations] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413174002_init-mig'
)
BEGIN
    CREATE TABLE [Teams] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [OrganizationId] int NOT NULL,
        CONSTRAINT [PK_Teams] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Teams_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413174002_init-mig'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [Username] nvarchar(100) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [PasswordHash] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [OrganizationId] int NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Users_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413174002_init-mig'
)
BEGIN
    CREATE TABLE [Tasks] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Status] int NOT NULL,
        [Priority] int NOT NULL,
        [DueDate] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [CreatedById] int NOT NULL,
        [AssignedToId] int NULL,
        [TeamId] int NULL,
        CONSTRAINT [PK_Tasks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Tasks_Teams_TeamId] FOREIGN KEY ([TeamId]) REFERENCES [Teams] ([Id]),
        CONSTRAINT [FK_Tasks_Users_AssignedToId] FOREIGN KEY ([AssignedToId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Tasks_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413174002_init-mig'
)
BEGIN
    CREATE TABLE [TeamUser] (
        [MembersId] int NOT NULL,
        [TeamsId] int NOT NULL,
        CONSTRAINT [PK_TeamUser] PRIMARY KEY ([MembersId], [TeamsId]),
        CONSTRAINT [FK_TeamUser_Teams_TeamsId] FOREIGN KEY ([TeamsId]) REFERENCES [Teams] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TeamUser_Users_MembersId] FOREIGN KEY ([MembersId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413174002_init-mig'
)
BEGIN
    CREATE INDEX [IX_Tasks_AssignedToId] ON [Tasks] ([AssignedToId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413174002_init-mig'
)
BEGIN
    CREATE INDEX [IX_Tasks_CreatedById] ON [Tasks] ([CreatedById]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413174002_init-mig'
)
BEGIN
    CREATE INDEX [IX_Tasks_TeamId] ON [Tasks] ([TeamId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413174002_init-mig'
)
BEGIN
    CREATE INDEX [IX_Teams_OrganizationId] ON [Teams] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413174002_init-mig'
)
BEGIN
    CREATE INDEX [IX_TeamUser_TeamsId] ON [TeamUser] ([TeamsId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413174002_init-mig'
)
BEGIN
    CREATE INDEX [IX_Users_OrganizationId] ON [Users] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413174002_init-mig'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260413174002_init-mig', N'8.0.25');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413182407_invitations'
)
BEGIN
    CREATE TABLE [Invitations] (
        [Id] int NOT NULL IDENTITY,
        [Email] nvarchar(max) NOT NULL,
        [OrganizationId] int NOT NULL,
        [TeamId] int NULL,
        [InvitedById] int NOT NULL,
        [Token] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [IsUsed] bit NOT NULL,
        CONSTRAINT [PK_Invitations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Invitations_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Invitations_Teams_TeamId] FOREIGN KEY ([TeamId]) REFERENCES [Teams] ([Id]),
        CONSTRAINT [FK_Invitations_Users_InvitedById] FOREIGN KEY ([InvitedById]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413182407_invitations'
)
BEGIN
    CREATE INDEX [IX_Invitations_InvitedById] ON [Invitations] ([InvitedById]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413182407_invitations'
)
BEGIN
    CREATE INDEX [IX_Invitations_OrganizationId] ON [Invitations] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413182407_invitations'
)
BEGIN
    CREATE INDEX [IX_Invitations_TeamId] ON [Invitations] ([TeamId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413182407_invitations'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260413182407_invitations', N'8.0.25');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504092856_TaskTimeWindowAndTeamOwner'
)
BEGIN
    EXEC sp_rename N'[Tasks].[DueDate]', N'PlannedEndAt', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504092856_TaskTimeWindowAndTeamOwner'
)
BEGIN
    ALTER TABLE [Teams] ADD [OwnerId] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504092856_TaskTimeWindowAndTeamOwner'
)
BEGIN

    UPDATE t
    SET t.OwnerId = tu.MembersId
    FROM Teams t
    INNER JOIN (
        SELECT TeamsId, MIN(MembersId) AS MembersId
        FROM TeamUser
        GROUP BY TeamsId
    ) tu ON tu.TeamsId = t.Id
    WHERE t.OwnerId = 0;

    UPDATE Teams
    SET OwnerId = (SELECT TOP(1) Id FROM Users ORDER BY Id)
    WHERE OwnerId = 0;

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504092856_TaskTimeWindowAndTeamOwner'
)
BEGIN
    ALTER TABLE [Tasks] ADD [CompletedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504092856_TaskTimeWindowAndTeamOwner'
)
BEGIN
    ALTER TABLE [Tasks] ADD [PlannedStartAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504092856_TaskTimeWindowAndTeamOwner'
)
BEGIN
    CREATE INDEX [IX_Teams_OwnerId] ON [Teams] ([OwnerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504092856_TaskTimeWindowAndTeamOwner'
)
BEGIN
    ALTER TABLE [Teams] ADD CONSTRAINT [FK_Teams_Users_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504092856_TaskTimeWindowAndTeamOwner'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260504092856_TaskTimeWindowAndTeamOwner', N'8.0.25');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504094950_RemoveInvitations'
)
BEGIN
    DROP TABLE [Invitations];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504094950_RemoveInvitations'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260504094950_RemoveInvitations', N'8.0.25');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504111436_TaskTeamSetNullOnDelete'
)
BEGIN
    ALTER TABLE [Tasks] DROP CONSTRAINT [FK_Tasks_Teams_TeamId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504111436_TaskTeamSetNullOnDelete'
)
BEGIN
    ALTER TABLE [Tasks] ADD CONSTRAINT [FK_Tasks_Teams_TeamId] FOREIGN KEY ([TeamId]) REFERENCES [Teams] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504111436_TaskTeamSetNullOnDelete'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260504111436_TaskTeamSetNullOnDelete', N'8.0.25');
END;
GO

COMMIT;
GO

