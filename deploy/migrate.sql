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
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [FullName] nvarchar(max) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
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
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE TABLE [Categories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE TABLE [CustomerProfiles] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [City] nvarchar(max) NOT NULL,
        [DefaultContact] nvarchar(max) NULL,
        CONSTRAINT [PK_CustomerProfiles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CustomerProfiles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE TABLE [ProviderProfiles] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [City] nvarchar(max) NOT NULL,
        [YearsOfExperience] int NOT NULL,
        [Bio] nvarchar(max) NULL,
        [IsApproved] bit NOT NULL,
        [AverageRating] decimal(3,2) NOT NULL,
        [ReviewCount] int NOT NULL,
        CONSTRAINT [PK_ProviderProfiles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProviderProfiles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE TABLE [Services] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [CategoryId] int NOT NULL,
        CONSTRAINT [PK_Services] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Services_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE TABLE [ProviderServices] (
        [Id] int NOT NULL IDENTITY,
        [ProviderProfileId] int NOT NULL,
        [ServiceId] int NOT NULL,
        CONSTRAINT [PK_ProviderServices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProviderServices_ProviderProfiles_ProviderProfileId] FOREIGN KEY ([ProviderProfileId]) REFERENCES [ProviderProfiles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProviderServices_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [Services] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE TABLE [ServiceRequests] (
        [Id] int NOT NULL IDENTITY,
        [CustomerId] nvarchar(450) NOT NULL,
        [ServiceId] int NOT NULL,
        [Title] nvarchar(120) NOT NULL,
        [Description] nvarchar(2000) NOT NULL,
        [City] nvarchar(80) NOT NULL,
        [PreferredDate] datetimeoffset NOT NULL,
        [BudgetMin] decimal(18,2) NULL,
        [BudgetMax] decimal(18,2) NULL,
        [Status] nvarchar(20) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_ServiceRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ServiceRequests_AspNetUsers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ServiceRequests_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [Services] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE TABLE [Offers] (
        [Id] int NOT NULL IDENTITY,
        [ServiceRequestId] int NOT NULL,
        [ProviderId] nvarchar(450) NOT NULL,
        [Price] decimal(18,2) NOT NULL,
        [Message] nvarchar(1000) NOT NULL,
        [EstimatedDate] datetimeoffset NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_Offers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Offers_AspNetUsers_ProviderId] FOREIGN KEY ([ProviderId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Offers_ServiceRequests_ServiceRequestId] FOREIGN KEY ([ServiceRequestId]) REFERENCES [ServiceRequests] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE TABLE [Bookings] (
        [Id] int NOT NULL IDENTITY,
        [OfferId] int NOT NULL,
        [ServiceRequestId] int NOT NULL,
        [CustomerId] nvarchar(450) NOT NULL,
        [ProviderId] nvarchar(450) NOT NULL,
        [ScheduledDate] datetimeoffset NOT NULL,
        [FinalPrice] decimal(18,2) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [StartedAt] datetimeoffset NULL,
        [CompletedAt] datetimeoffset NULL,
        [CancelledAt] datetimeoffset NULL,
        [CancellationReason] nvarchar(max) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Bookings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Bookings_AspNetUsers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Bookings_AspNetUsers_ProviderId] FOREIGN KEY ([ProviderId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Bookings_Offers_OfferId] FOREIGN KEY ([OfferId]) REFERENCES [Offers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Bookings_ServiceRequests_ServiceRequestId] FOREIGN KEY ([ServiceRequestId]) REFERENCES [ServiceRequests] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE TABLE [Reviews] (
        [Id] int NOT NULL IDENTITY,
        [BookingId] int NOT NULL,
        [CustomerId] nvarchar(450) NOT NULL,
        [ProviderId] nvarchar(450) NOT NULL,
        [Rating] int NOT NULL,
        [Comment] nvarchar(1000) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_Reviews] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Review_Rating] CHECK ([Rating] BETWEEN 1 AND 5),
        CONSTRAINT [FK_Reviews_AspNetUsers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Reviews_AspNetUsers_ProviderId] FOREIGN KEY ([ProviderId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Reviews_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Bookings_CustomerId_Status] ON [Bookings] ([CustomerId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Bookings_OfferId] ON [Bookings] ([OfferId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Bookings_ProviderId_Status] ON [Bookings] ([ProviderId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Bookings_ServiceRequestId] ON [Bookings] ([ServiceRequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CustomerProfiles_UserId] ON [CustomerProfiles] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Offers_ProviderId_Status] ON [Offers] ([ProviderId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Offers_ServiceRequestId_ProviderId] ON [Offers] ([ServiceRequestId], [ProviderId]) WHERE [Status] <> ''Withdrawn''');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Offer_OneAcceptedPerRequest] ON [Offers] ([ServiceRequestId]) WHERE [Status] = ''Accepted''');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProviderProfiles_UserId] ON [ProviderProfiles] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProviderServices_ProviderProfileId_ServiceId] ON [ProviderServices] ([ProviderProfileId], [ServiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ProviderServices_ServiceId] ON [ProviderServices] ([ServiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Reviews_BookingId] ON [Reviews] ([BookingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Reviews_CustomerId] ON [Reviews] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Reviews_ProviderId] ON [Reviews] ([ProviderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ServiceRequests_CustomerId_Status] ON [ServiceRequests] ([CustomerId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ServiceRequests_ServiceId] ON [ServiceRequests] ([ServiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ServiceRequests_Status_ServiceId_City] ON [ServiceRequests] ([Status], [ServiceId], [City]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Services_CategoryId] ON [Services] ([CategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904230532_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260904230532_InitialCreate', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916111438_AddProviderVerificationAuditAndSuspension'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ProviderProfiles]') AND [c].[name] = N'City');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [ProviderProfiles] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [ProviderProfiles] ALTER COLUMN [City] nvarchar(80) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916111438_AddProviderVerificationAuditAndSuspension'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ProviderProfiles]') AND [c].[name] = N'Bio');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [ProviderProfiles] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [ProviderProfiles] ALTER COLUMN [Bio] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916111438_AddProviderVerificationAuditAndSuspension'
)
BEGIN
    ALTER TABLE [ProviderProfiles] ADD [IsSuspended] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916111438_AddProviderVerificationAuditAndSuspension'
)
BEGIN
    ALTER TABLE [ProviderProfiles] ADD [SuspendedAt] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916111438_AddProviderVerificationAuditAndSuspension'
)
BEGIN
    ALTER TABLE [ProviderProfiles] ADD [SuspendedByUserId] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916111438_AddProviderVerificationAuditAndSuspension'
)
BEGIN
    ALTER TABLE [ProviderProfiles] ADD [SuspensionReason] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916111438_AddProviderVerificationAuditAndSuspension'
)
BEGIN
    ALTER TABLE [ProviderProfiles] ADD [VerificationRejectionReason] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916111438_AddProviderVerificationAuditAndSuspension'
)
BEGIN
    ALTER TABLE [ProviderProfiles] ADD [VerificationReviewedAt] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916111438_AddProviderVerificationAuditAndSuspension'
)
BEGIN
    ALTER TABLE [ProviderProfiles] ADD [VerificationReviewedByUserId] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916111438_AddProviderVerificationAuditAndSuspension'
)
BEGIN
    ALTER TABLE [ProviderProfiles] ADD [VerificationStatus] nvarchar(32) NOT NULL DEFAULT N'PendingReview';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916111438_AddProviderVerificationAuditAndSuspension'
)
BEGIN
    UPDATE ProviderProfiles
    SET VerificationStatus = CASE
        WHEN IsApproved = 1 THEN 'Approved'
        ELSE 'PendingReview'
    END;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916111438_AddProviderVerificationAuditAndSuspension'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ProviderProfiles]') AND [c].[name] = N'IsApproved');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [ProviderProfiles] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [ProviderProfiles] DROP COLUMN [IsApproved];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916111438_AddProviderVerificationAuditAndSuspension'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUsers]') AND [c].[name] = N'FullName');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUsers] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [AspNetUsers] ALTER COLUMN [FullName] nvarchar(200) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916111438_AddProviderVerificationAuditAndSuspension'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [LastLoginAt] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916111438_AddProviderVerificationAuditAndSuspension'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [CreatedAt] datetimeoffset NOT NULL,
        [ActorUserId] nvarchar(450) NULL,
        [ActorEmail] nvarchar(256) NULL,
        [ActorRole] nvarchar(50) NULL,
        [Category] nvarchar(50) NOT NULL,
        [Action] nvarchar(100) NOT NULL,
        [EntityType] nvarchar(100) NULL,
        [EntityId] nvarchar(100) NULL,
        [Outcome] nvarchar(20) NOT NULL,
        [Message] nvarchar(1000) NULL,
        [DetailsJson] nvarchar(max) NULL,
        [IpAddress] nvarchar(64) NULL,
        [UserAgent] nvarchar(512) NULL,
        [CorrelationId] nvarchar(64) NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916111438_AddProviderVerificationAuditAndSuspension'
)
BEGIN
    CREATE TABLE [ProviderVerificationDocuments] (
        [Id] int NOT NULL IDENTITY,
        [ProviderProfileId] int NOT NULL,
        [DocumentType] nvarchar(40) NOT NULL,
        [OriginalFileName] nvarchar(260) NOT NULL,
        [StoredFileName] nvarchar(100) NOT NULL,
        [ContentType] nvarchar(100) NOT NULL,
        [FileSizeBytes] bigint NOT NULL,
        [UploadedAt] datetimeoffset NOT NULL,
        [ReviewStatus] nvarchar(20) NOT NULL,
        [ReviewNote] nvarchar(1000) NULL,
        [ReviewedAt] datetimeoffset NULL,
        [ReviewedByUserId] nvarchar(450) NULL,
        CONSTRAINT [PK_ProviderVerificationDocuments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ProviderVerificationDocuments_FileSize] CHECK (FileSizeBytes > 0),
        CONSTRAINT [FK_ProviderVerificationDocuments_ProviderProfiles_ProviderProfileId] FOREIGN KEY ([ProviderProfileId]) REFERENCES [ProviderProfiles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916111438_AddProviderVerificationAuditAndSuspension'
)
BEGIN
    CREATE INDEX [IX_ProviderProfiles_IsSuspended] ON [ProviderProfiles] ([IsSuspended]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916111438_AddProviderVerificationAuditAndSuspension'
)
BEGIN
    CREATE INDEX [IX_ProviderProfiles_VerificationStatus] ON [ProviderProfiles] ([VerificationStatus]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916111438_AddProviderVerificationAuditAndSuspension'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_Action_CreatedAt] ON [AuditLogs] ([Action], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916111438_AddProviderVerificationAuditAndSuspension'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_ActorUserId_CreatedAt] ON [AuditLogs] ([ActorUserId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916111438_AddProviderVerificationAuditAndSuspension'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_Category_CreatedAt] ON [AuditLogs] ([Category], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916111438_AddProviderVerificationAuditAndSuspension'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_CreatedAt] ON [AuditLogs] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916111438_AddProviderVerificationAuditAndSuspension'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_EntityType_EntityId] ON [AuditLogs] ([EntityType], [EntityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916111438_AddProviderVerificationAuditAndSuspension'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_Outcome_CreatedAt] ON [AuditLogs] ([Outcome], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916111438_AddProviderVerificationAuditAndSuspension'
)
BEGIN
    CREATE INDEX [IX_ProviderVerificationDocuments_ProviderProfileId_ReviewStatus] ON [ProviderVerificationDocuments] ([ProviderProfileId], [ReviewStatus]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916111438_AddProviderVerificationAuditAndSuspension'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProviderVerificationDocuments_StoredFileName] ON [ProviderVerificationDocuments] ([StoredFileName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916111438_AddProviderVerificationAuditAndSuspension'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260916111438_AddProviderVerificationAuditAndSuspension', N'10.0.11');
END;

COMMIT;
GO

