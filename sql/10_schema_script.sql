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
    WHERE [MigrationId] = N'20261101135730_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditEntries] (
        [Id] uniqueidentifier NOT NULL,
        [StaffId] uniqueidentifier NOT NULL,
        [Role] nvarchar(64) NOT NULL,
        [BranchCode] nvarchar(32) NOT NULL,
        [Controller] nvarchar(128) NOT NULL,
        [Action] nvarchar(128) NOT NULL,
        [CorrelationId] nvarchar(128) NOT NULL,
        [ResultStatus] nvarchar(64) NOT NULL,
        [RouteValues] nvarchar(512) NOT NULL,
        [DurationMs] float NOT NULL,
        [RecordedAtUtc] datetimeoffset NOT NULL,
        [Details] nvarchar(1024) NOT NULL,
        [RedactedFields] nvarchar(max) NOT NULL,
        [TransactionId] nvarchar(64) NULL,
        [Reason] nvarchar(300) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [ModifiedAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_AuditEntries] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101135730_InitialCreate'
)
BEGIN
    CREATE TABLE [Branches] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(32) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Region] nvarchar(100) NOT NULL,
        [OpeningHours] nvarchar(100) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [Counters] nvarchar(max) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [ModifiedAtUtc] datetimeoffset NULL,
        [CreatedBy] nvarchar(128) NULL DEFAULT N'system',
        [ModifiedBy] nvarchar(128) NULL,
        CONSTRAINT [PK_Branches] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101135730_InitialCreate'
)
BEGIN
    CREATE TABLE [BranchStock] (
        [Id] uniqueidentifier NOT NULL,
        [BranchCode] nvarchar(32) NOT NULL,
        [ProductSku] nvarchar(64) NOT NULL,
        [QuantityOnHand] int NOT NULL,
        [ReorderThreshold] int NOT NULL DEFAULT 5,
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [ModifiedAtUtc] datetimeoffset NULL,
        [CreatedBy] nvarchar(128) NULL DEFAULT N'system',
        [ModifiedBy] nvarchar(128) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_BranchStock] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_BranchStock_QuantityOnHand_NonNegative] CHECK (QuantityOnHand >= 0),
        CONSTRAINT [CK_BranchStock_ReorderThreshold_NonNegative] CHECK (ReorderThreshold >= 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101135730_InitialCreate'
)
BEGIN
    CREATE TABLE [CounterTransactions] (
        [Id] uniqueidentifier NOT NULL,
        [BranchCode] nvarchar(32) NOT NULL,
        [CounterId] nvarchar(32) NOT NULL,
        [ClerkId] uniqueidentifier NOT NULL,
        [PaymentMethod] nvarchar(32) NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [SubTotal] decimal(18,2) NOT NULL,
        [TaxTotal] decimal(18,2) NOT NULL,
        [Total] decimal(18,2) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [ModifiedAtUtc] datetimeoffset NULL,
        [ClosedAtUtc] datetimeoffset NULL,
        [VoidReason] nvarchar(200) NULL,
        [AuditCorrelationId] nvarchar(128) NULL,
        [CreatedBy] nvarchar(128) NULL DEFAULT N'system',
        [ModifiedBy] nvarchar(128) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_CounterTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CounterTransactions_Status_Valid] CHECK (Status IN ('Open','Paid','Voided','Refunded')),
        CONSTRAINT [CK_CounterTransactions_Total_Positive] CHECK (Total >= 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101135730_InitialCreate'
)
BEGIN
    CREATE TABLE [IdentityVerifications] (
        [Id] uniqueidentifier NOT NULL,
        [StaffId] uniqueidentifier NOT NULL,
        [TransactionId] uniqueidentifier NOT NULL,
        [BranchCode] nvarchar(32) NOT NULL,
        [DocumentType] nvarchar(64) NOT NULL,
        [IsVerified] bit NOT NULL DEFAULT CAST(0 AS bit),
        [MaskedIdentifier] nvarchar(20) NOT NULL,
        [VerifiedAtUtc] datetimeoffset NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [ModifiedAtUtc] datetimeoffset NULL,
        [CreatedBy] nvarchar(128) NULL DEFAULT N'system',
        [ModifiedBy] nvarchar(128) NULL,
        CONSTRAINT [PK_IdentityVerifications] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101135730_InitialCreate'
)
BEGIN
    CREATE TABLE [MoneyOrders] (
        [Id] uniqueidentifier NOT NULL,
        [TransactionId] uniqueidentifier NOT NULL,
        [BranchCode] nvarchar(32) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Sender] nvarchar(200) NOT NULL,
        [Recipient] nvarchar(200) NOT NULL,
        [Fee] decimal(18,2) NOT NULL,
        [RequiresIdentityVerification] bit NOT NULL DEFAULT CAST(0 AS bit),
        [IdentityVerificationComplete] bit NOT NULL DEFAULT CAST(0 AS bit),
        [Status] nvarchar(32) NOT NULL DEFAULT N'Pending',
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [ModifiedAtUtc] datetimeoffset NULL,
        [CreatedBy] nvarchar(128) NULL DEFAULT N'system',
        [ModifiedBy] nvarchar(128) NULL,
        CONSTRAINT [PK_MoneyOrders] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_MoneyOrders_Amount_Positive] CHECK (Amount > 0),
        CONSTRAINT [CK_MoneyOrders_Fee_NonNegative] CHECK (Fee >= 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101135730_InitialCreate'
)
BEGIN
    CREATE TABLE [Products] (
        [Id] uniqueidentifier NOT NULL,
        [Sku] nvarchar(64) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Category] nvarchar(32) NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [TaxCategory] nvarchar(32) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [ModifiedAtUtc] datetimeoffset NULL,
        [CreatedBy] nvarchar(128) NULL DEFAULT N'system',
        [ModifiedBy] nvarchar(128) NULL,
        CONSTRAINT [PK_Products] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101135730_InitialCreate'
)
BEGIN
    CREATE TABLE [Staff] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Email] nvarchar(200) NOT NULL,
        [PasswordHash] nvarchar(512) NOT NULL,
        [Role] nvarchar(32) NOT NULL,
        [HomeBranch] nvarchar(32) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [ModifiedAtUtc] datetimeoffset NULL,
        [CreatedBy] nvarchar(128) NULL DEFAULT N'system',
        [ModifiedBy] nvarchar(128) NULL,
        CONSTRAINT [PK_Staff] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101135730_InitialCreate'
)
BEGIN
    CREATE TABLE [TransactionLines] (
        [Id] uniqueidentifier NOT NULL,
        [Description] nvarchar(200) NOT NULL,
        [ProductSku] nvarchar(64) NULL,
        [ServiceCode] nvarchar(64) NULL,
        [Quantity] int NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [TaxCategory] nvarchar(32) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [ModifiedAtUtc] datetimeoffset NULL,
        [CounterTransactionId] uniqueidentifier NOT NULL,
        [CreatedBy] nvarchar(128) NULL DEFAULT N'system',
        [ModifiedBy] nvarchar(128) NULL,
        CONSTRAINT [PK_TransactionLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_TransactionLines_Quantity_Positive] CHECK (Quantity > 0),
        CONSTRAINT [FK_TransactionLines_CounterTransactions_CounterTransactionId] FOREIGN KEY ([CounterTransactionId]) REFERENCES [CounterTransactions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101135730_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Branches_Code] ON [Branches] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101135730_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BranchStock_BranchCode_ProductSku] ON [BranchStock] ([BranchCode], [ProductSku]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101135730_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Products_Sku] ON [Products] ([Sku]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101135730_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Staff_Email] ON [Staff] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101135730_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TransactionLines_CounterTransactionId] ON [TransactionLines] ([CounterTransactionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101135730_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261101135730_InitialCreate', N'10.0.0');
END;

COMMIT;
GO

