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
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812191246_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812191246_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] uniqueidentifier NOT NULL,
        [DisplayName] nvarchar(200) NULL,
        [IsPlatformAdmin] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812191246_InitialCreate'
)
BEGIN
    CREATE TABLE [Groups] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Groups] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812191246_InitialCreate'
)
BEGIN
    CREATE TABLE [Permissions] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(100) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812191246_InitialCreate'
)
BEGIN
    CREATE TABLE [PlatformNotices] (
        [Id] uniqueidentifier NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Body] nvarchar(4000) NOT NULL,
        [IsPublished] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_PlatformNotices] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812191246_InitialCreate'
)
BEGIN
    CREATE TABLE [RefreshTokens] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [TokenHash] nvarchar(128) NOT NULL,
        [ExpiresAtUtc] datetime2 NOT NULL,
        [RevokedAtUtc] datetime2 NULL,
        [ReplacedByTokenHash] nvarchar(max) NULL,
        [TenantId] uniqueidentifier NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812191246_InitialCreate'
)
BEGIN
    CREATE TABLE [Tenants] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Slug] nvarchar(100) NOT NULL,
        [Document] nvarchar(32) NULL,
        [Status] int NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Tenants] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812191246_InitialCreate'
)
BEGIN
    CREATE TABLE [UserGroupMemberships] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [GroupId] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_UserGroupMemberships] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812191246_InitialCreate'
)
BEGIN
    CREATE TABLE [UserTenantMemberships] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [IsDefault] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_UserTenantMemberships] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812191246_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812191246_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812191246_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812191246_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] uniqueidentifier NOT NULL,
        [RoleId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812191246_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] uniqueidentifier NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812191246_InitialCreate'
)
BEGIN
    CREATE TABLE [GroupPermissions] (
        [Id] uniqueidentifier NOT NULL,
        [GroupId] uniqueidentifier NOT NULL,
        [PermissionId] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_GroupPermissions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GroupPermissions_Groups_GroupId] FOREIGN KEY ([GroupId]) REFERENCES [Groups] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812191246_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812191246_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812191246_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812191246_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812191246_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812191246_InitialCreate'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812191246_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812191246_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_GroupPermissions_GroupId_PermissionId] ON [GroupPermissions] ([GroupId], [PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812191246_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Groups_TenantId_Name] ON [Groups] ([TenantId], [Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812191246_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Permissions_Code] ON [Permissions] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812191246_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [RefreshTokens] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812191246_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812191246_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Tenants_Slug] ON [Tenants] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812191246_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserGroupMemberships_TenantId] ON [UserGroupMemberships] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812191246_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserGroupMemberships_UserId_TenantId_GroupId] ON [UserGroupMemberships] ([UserId], [TenantId], [GroupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812191246_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserTenantMemberships_TenantId] ON [UserTenantMemberships] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812191246_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserTenantMemberships_UserId_TenantId] ON [UserTenantMemberships] ([UserId], [TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812191246_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260812191246_InitialCreate', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812221000_AddAuditLogs'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [UserId] uniqueidentifier NULL,
        [Action] nvarchar(32) NOT NULL,
        [EntityName] nvarchar(128) NOT NULL,
        [EntityId] uniqueidentifier NOT NULL,
        [OccurredAtUtc] datetime2 NOT NULL,
        [JsonChanges] nvarchar(max) NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812221000_AddAuditLogs'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_EntityName_EntityId] ON [AuditLogs] ([EntityName], [EntityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812221000_AddAuditLogs'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_OccurredAtUtc] ON [AuditLogs] ([OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812221000_AddAuditLogs'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_TenantId] ON [AuditLogs] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812221000_AddAuditLogs'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_UserId] ON [AuditLogs] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812221000_AddAuditLogs'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260812221000_AddAuditLogs', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814192446_AddTenantRegistrationFields'
)
BEGIN
    ALTER TABLE [Tenants] ADD [AddressCity] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814192446_AddTenantRegistrationFields'
)
BEGIN
    ALTER TABLE [Tenants] ADD [AddressComplement] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814192446_AddTenantRegistrationFields'
)
BEGIN
    ALTER TABLE [Tenants] ADD [AddressNeighborhood] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814192446_AddTenantRegistrationFields'
)
BEGIN
    ALTER TABLE [Tenants] ADD [AddressNumber] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814192446_AddTenantRegistrationFields'
)
BEGIN
    ALTER TABLE [Tenants] ADD [AddressState] nvarchar(2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814192446_AddTenantRegistrationFields'
)
BEGIN
    ALTER TABLE [Tenants] ADD [AddressStreet] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814192446_AddTenantRegistrationFields'
)
BEGIN
    ALTER TABLE [Tenants] ADD [AddressZipCode] nvarchar(8) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814192446_AddTenantRegistrationFields'
)
BEGIN
    ALTER TABLE [Tenants] ADD [BrandDescription] nvarchar(4000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814192446_AddTenantRegistrationFields'
)
BEGIN
    ALTER TABLE [Tenants] ADD [BrandName] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814192446_AddTenantRegistrationFields'
)
BEGIN
    ALTER TABLE [Tenants] ADD [Email] nvarchar(256) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814192446_AddTenantRegistrationFields'
)
BEGIN
    ALTER TABLE [Tenants] ADD [Phone] nvarchar(32) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814192446_AddTenantRegistrationFields'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Tenants_Document] ON [Tenants] ([Document]) WHERE [Document] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814192446_AddTenantRegistrationFields'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Tenants_Email] ON [Tenants] ([Email]) WHERE [Email] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814192446_AddTenantRegistrationFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260814192446_AddTenantRegistrationFields', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260817164000_AllowDuplicateTenantDocuments'
)
BEGIN
    DROP INDEX [IX_Tenants_Document] ON [Tenants];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260817164000_AllowDuplicateTenantDocuments'
)
BEGIN
    CREATE INDEX [IX_Tenants_Document] ON [Tenants] ([Document]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260817164000_AllowDuplicateTenantDocuments'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260817164000_AllowDuplicateTenantDocuments', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824151157_AddCommercialFunnel'
)
BEGIN
    CREATE TABLE [Brands] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [ServicesDescription] nvarchar(4000) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Brands] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824151157_AddCommercialFunnel'
)
BEGIN
    INSERT INTO [Brands] ([Id], [TenantId], [Name], [ServicesDescription], [CreatedAtUtc], [UpdatedAtUtc], [IsDeleted], [DeletedAtUtc])
    SELECT NEWID(), [Id], [BrandName], ISNULL([BrandDescription], N''), [CreatedAtUtc], [UpdatedAtUtc], [IsDeleted], [DeletedAtUtc]
    FROM [Tenants]
    WHERE [BrandName] IS NOT NULL AND LTRIM(RTRIM([BrandName])) <> N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824151157_AddCommercialFunnel'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Tenants]') AND [c].[name] = N'BrandName');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Tenants] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Tenants] DROP COLUMN [BrandName];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824151157_AddCommercialFunnel'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Tenants]') AND [c].[name] = N'BrandDescription');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Tenants] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [Tenants] DROP COLUMN [BrandDescription];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824151157_AddCommercialFunnel'
)
BEGIN
    ALTER TABLE [Tenants] ADD [LegalRepresentativeName] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824151157_AddCommercialFunnel'
)
BEGIN
    ALTER TABLE [Tenants] ADD [LegalRepresentativeDocument] nvarchar(32) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824151157_AddCommercialFunnel'
)
BEGIN
    ALTER TABLE [Tenants] ADD [QuotedAmount] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824151157_AddCommercialFunnel'
)
BEGIN
    ALTER TABLE [Tenants] ADD [BudgetNotes] nvarchar(4000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824151157_AddCommercialFunnel'
)
BEGIN
    CREATE TABLE [Contracts] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [BrandId] uniqueidentifier NOT NULL,
        [Code] nvarchar(32) NOT NULL,
        [CompanyDocument] nvarchar(32) NOT NULL,
        [RepresentativeDocument] nvarchar(32) NOT NULL,
        [BrandName] nvarchar(200) NOT NULL,
        [Classes] nvarchar(200) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Status] int NOT NULL,
        [FormarcaSignedAt] datetime2 NOT NULL,
        [ClientSignedAt] datetime2 NULL,
        [GeneratedAtUtc] datetime2 NOT NULL,
        [SignedAtUtc] datetime2 NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Contracts] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824151157_AddCommercialFunnel'
)
BEGIN
    CREATE TABLE [Invoices] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [ContractId] uniqueidentifier NOT NULL,
        [PaymentMethod] int NULL,
        [Installments] int NULL,
        [ContractAmount] decimal(18,2) NOT NULL,
        [Discount] decimal(9,4) NOT NULL,
        [ExpectedAmount] decimal(18,2) NOT NULL,
        [ReceivedAmount] decimal(18,2) NOT NULL,
        [Status] int NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Invoices] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824151157_AddCommercialFunnel'
)
BEGIN
    CREATE TABLE [InvoiceSettings] (
        [Id] uniqueidentifier NOT NULL,
        [InterestRatesJson] nvarchar(4000) NOT NULL,
        [PixDiscount] decimal(9,4) NOT NULL,
        [GeneralDiscount] decimal(9,4) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_InvoiceSettings] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824151157_AddCommercialFunnel'
)
BEGIN
    CREATE TABLE [Processes] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [BrandId] uniqueidentifier NOT NULL,
        [ContractId] uniqueidentifier NOT NULL,
        [Status] int NOT NULL,
        [ProcessNumber] nvarchar(64) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Processes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824151157_AddCommercialFunnel'
)
BEGIN
    CREATE TABLE [BrandNiceClasses] (
        [Id] uniqueidentifier NOT NULL,
        [Number] int NOT NULL,
        [BrandId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_BrandNiceClasses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BrandNiceClasses_Brands_BrandId] FOREIGN KEY ([BrandId]) REFERENCES [Brands] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824151157_AddCommercialFunnel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BrandNiceClasses_BrandId_Number] ON [BrandNiceClasses] ([BrandId], [Number]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824151157_AddCommercialFunnel'
)
BEGIN
    CREATE INDEX [IX_Brands_TenantId] ON [Brands] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824151157_AddCommercialFunnel'
)
BEGIN
    CREATE INDEX [IX_Brands_TenantId_Name] ON [Brands] ([TenantId], [Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824151157_AddCommercialFunnel'
)
BEGIN
    CREATE INDEX [IX_Contracts_BrandId] ON [Contracts] ([BrandId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824151157_AddCommercialFunnel'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Contracts_Code] ON [Contracts] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824151157_AddCommercialFunnel'
)
BEGIN
    CREATE INDEX [IX_Contracts_TenantId] ON [Contracts] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824151157_AddCommercialFunnel'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Invoices_ContractId] ON [Invoices] ([ContractId]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824151157_AddCommercialFunnel'
)
BEGIN
    CREATE INDEX [IX_Invoices_TenantId] ON [Invoices] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824151157_AddCommercialFunnel'
)
BEGIN
    CREATE INDEX [IX_Processes_BrandId] ON [Processes] ([BrandId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824151157_AddCommercialFunnel'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Processes_ContractId] ON [Processes] ([ContractId]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824151157_AddCommercialFunnel'
)
BEGIN
    CREATE INDEX [IX_Processes_TenantId] ON [Processes] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824151157_AddCommercialFunnel'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260824151157_AddCommercialFunnel', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824155055_AddUserTableViews'
)
BEGIN
    CREATE TABLE [UserTableViews] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [TableKey] nvarchar(50) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [ColumnKeysJson] nvarchar(4000) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_UserTableViews] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824155055_AddUserTableViews'
)
BEGIN
    CREATE INDEX [IX_UserTableViews_UserId_TableKey] ON [UserTableViews] ([UserId], [TableKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824155055_AddUserTableViews'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_UserTableViews_UserId_TableKey_Name] ON [UserTableViews] ([UserId], [TableKey], [Name]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824155055_AddUserTableViews'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260824155055_AddUserTableViews', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902174156_AddContractDocuments'
)
BEGIN
    ALTER TABLE [Contracts] ADD [BodyMarkdown] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902174156_AddContractDocuments'
)
BEGIN
    ALTER TABLE [Contracts] ADD [City] nvarchar(100) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902174156_AddContractDocuments'
)
BEGIN
    ALTER TABLE [Contracts] ADD [ClientSignaturePng] varbinary(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902174156_AddContractDocuments'
)
BEGIN
    ALTER TABLE [Contracts] ADD [CompanyName] nvarchar(200) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902174156_AddContractDocuments'
)
BEGIN
    ALTER TABLE [Contracts] ADD [MinimumTerm] nvarchar(100) NOT NULL DEFAULT N'12 (doze) meses';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902174156_AddContractDocuments'
)
BEGIN
    ALTER TABLE [Contracts] ADD [Number] nvarchar(20) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902174156_AddContractDocuments'
)
BEGIN
    ALTER TABLE [Contracts] ADD [State] nvarchar(2) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902174156_AddContractDocuments'
)
BEGIN
    ALTER TABLE [Contracts] ADD [Street] nvarchar(200) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902174156_AddContractDocuments'
)
BEGIN
    ALTER TABLE [Contracts] ADD [TemplateKey] nvarchar(64) NOT NULL DEFAULT N'prestacao-servicos';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902174156_AddContractDocuments'
)
BEGIN
    ALTER TABLE [Contracts] ADD [ZipCode] nvarchar(8) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902174156_AddContractDocuments'
)
BEGIN
    CREATE TABLE [ContractFiles] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [ContractId] uniqueidentifier NOT NULL,
        [Kind] int NOT NULL,
        [FileName] nvarchar(128) NOT NULL,
        [ContentType] nvarchar(128) NOT NULL,
        [Content] varbinary(max) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_ContractFiles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902174156_AddContractDocuments'
)
BEGIN
    CREATE INDEX [IX_ContractFiles_ContractId] ON [ContractFiles] ([ContractId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902174156_AddContractDocuments'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ContractFiles_ContractId_Kind] ON [ContractFiles] ([ContractId], [Kind]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902174156_AddContractDocuments'
)
BEGIN
    CREATE INDEX [IX_ContractFiles_TenantId] ON [ContractFiles] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902174156_AddContractDocuments'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260902174156_AddContractDocuments', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902181617_AddAutentiqueContractFields'
)
BEGIN
    ALTER TABLE [Contracts] ADD [AutentiqueDocumentId] nvarchar(128) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902181617_AddAutentiqueContractFields'
)
BEGIN
    ALTER TABLE [Contracts] ADD [AutentiqueSignUrl] nvarchar(2048) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902181617_AddAutentiqueContractFields'
)
BEGIN
    ALTER TABLE [Contracts] ADD [AutentiqueSignerPublicId] nvarchar(64) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902181617_AddAutentiqueContractFields'
)
BEGIN
    ALTER TABLE [Contracts] ADD [AutentiqueStatus] nvarchar(32) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902181617_AddAutentiqueContractFields'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Contracts_AutentiqueDocumentId] ON [Contracts] ([AutentiqueDocumentId]) WHERE [AutentiqueDocumentId] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902181617_AddAutentiqueContractFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260902181617_AddAutentiqueContractFields', N'10.0.11');
END;

COMMIT;
GO

