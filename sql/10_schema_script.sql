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
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
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
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE TABLE [Branches] (
        [Id] uniqueidentifier NOT NULL,
        [Code] varchar(32) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Region] nvarchar(100) NOT NULL,
        [OpeningHours] nvarchar(100) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [ModifiedAtUtc] datetimeoffset NULL,
        [CreatedBy] nvarchar(128) NULL DEFAULT N'system',
        [ModifiedBy] nvarchar(128) NULL,
        CONSTRAINT [PK_Branches] PRIMARY KEY ([Id]),
        CONSTRAINT [AK_Branches_Code] UNIQUE ([Code])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE TABLE [ProductCategories] (
        [Id] varchar(32) NOT NULL,
        [DisplayName] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_ProductCategories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE TABLE [Roles] (
        [Id] varchar(32) NOT NULL,
        [DisplayName] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE TABLE [ShippingZones] (
        [Id] int NOT NULL,
        [Code] varchar(16) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Region] nvarchar(100) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(128) NULL DEFAULT N'system',
        [ModifiedAtUtc] datetimeoffset NULL,
        [ModifiedBy] nvarchar(128) NULL,
        CONSTRAINT [PK_ShippingZones] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE TABLE [Counters] (
        [Id] uniqueidentifier NOT NULL,
        [BranchCode] varchar(32) NOT NULL,
        [CounterCode] varchar(32) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(128) NULL DEFAULT N'system',
        [ModifiedAtUtc] datetimeoffset NULL,
        [ModifiedBy] nvarchar(128) NULL,
        CONSTRAINT [PK_Counters] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Counters_Branches_BranchCode] FOREIGN KEY ([BranchCode]) REFERENCES [Branches] ([Code]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE TABLE [Products] (
        [Id] uniqueidentifier NOT NULL,
        [Sku] varchar(64) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Category] varchar(32) NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [TaxCategory] varchar(32) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [ModifiedAtUtc] datetimeoffset NULL,
        [CreatedBy] nvarchar(128) NULL DEFAULT N'system',
        [ModifiedBy] nvarchar(128) NULL,
        CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
        CONSTRAINT [AK_Products_Sku] UNIQUE ([Sku]),
        CONSTRAINT [CK_Products_UnitPrice_NonNegative] CHECK (UnitPrice >= 0),
        CONSTRAINT [FK_Products_ProductCategories_Category] FOREIGN KEY ([Category]) REFERENCES [ProductCategories] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE TABLE [Staff] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Email] nvarchar(200) NOT NULL,
        [PasswordHash] nvarchar(512) NOT NULL,
        [Role] varchar(32) NOT NULL,
        [HomeBranch] varchar(32) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [ModifiedAtUtc] datetimeoffset NULL,
        [CreatedBy] nvarchar(128) NULL DEFAULT N'system',
        [ModifiedBy] nvarchar(128) NULL,
        CONSTRAINT [PK_Staff] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Staff_Branches_HomeBranch] FOREIGN KEY ([HomeBranch]) REFERENCES [Branches] ([Code]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Staff_Roles_Role] FOREIGN KEY ([Role]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE TABLE [ShippingRates] (
        [Id] int NOT NULL,
        [ZoneId] int NOT NULL,
        [ServiceLevel] varchar(32) NOT NULL,
        [MaxWeightKg] decimal(8,2) NOT NULL,
        [BaseRate] decimal(18,2) NOT NULL,
        [PerKgRate] decimal(18,2) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(128) NULL DEFAULT N'system',
        [ModifiedAtUtc] datetimeoffset NULL,
        [ModifiedBy] nvarchar(128) NULL,
        CONSTRAINT [PK_ShippingRates] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ShippingRates_MaxWeight_Range] CHECK (MaxWeightKg > 0 AND MaxWeightKg <= 30),
        CONSTRAINT [CK_ShippingRates_Rates_NonNegative] CHECK (BaseRate >= 0 AND PerKgRate >= 0),
        CONSTRAINT [FK_ShippingRates_ShippingZones_ZoneId] FOREIGN KEY ([ZoneId]) REFERENCES [ShippingZones] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE TABLE [BranchStock] (
        [Id] uniqueidentifier NOT NULL,
        [BranchCode] varchar(32) NOT NULL,
        [ProductSku] varchar(64) NOT NULL,
        [QuantityOnHand] int NOT NULL,
        [ReorderThreshold] int NOT NULL DEFAULT 5,
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [ModifiedAtUtc] datetimeoffset NULL,
        [CreatedBy] nvarchar(128) NULL DEFAULT N'system',
        [ModifiedBy] nvarchar(128) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_BranchStock] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_BranchStock_QuantityOnHand_NonNegative] CHECK (QuantityOnHand >= 0),
        CONSTRAINT [CK_BranchStock_ReorderThreshold_NonNegative] CHECK (ReorderThreshold >= 0),
        CONSTRAINT [FK_BranchStock_Branches_BranchCode] FOREIGN KEY ([BranchCode]) REFERENCES [Branches] ([Code]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BranchStock_Products_ProductSku] FOREIGN KEY ([ProductSku]) REFERENCES [Products] ([Sku]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE TABLE [StockAdjustments] (
        [Id] int NOT NULL IDENTITY,
        [BranchCode] varchar(32) NOT NULL,
        [ProductSku] varchar(64) NOT NULL,
        [Delta] int NOT NULL,
        [Reason] nvarchar(200) NOT NULL,
        [ChangedBy] nvarchar(128) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(128) NULL DEFAULT N'system',
        [ModifiedAtUtc] datetimeoffset NULL,
        [ModifiedBy] nvarchar(128) NULL,
        CONSTRAINT [PK_StockAdjustments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_StockAdjustments_Delta_NonZero] CHECK (Delta <> 0),
        CONSTRAINT [FK_StockAdjustments_Branches_BranchCode] FOREIGN KEY ([BranchCode]) REFERENCES [Branches] ([Code]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockAdjustments_Products_ProductSku] FOREIGN KEY ([ProductSku]) REFERENCES [Products] ([Sku]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE TABLE [CounterTransactions] (
        [Id] uniqueidentifier NOT NULL,
        [BranchCode] varchar(32) NOT NULL,
        [CounterId] varchar(32) NOT NULL,
        [ClerkId] uniqueidentifier NOT NULL,
        [PaymentMethod] varchar(32) NOT NULL,
        [Status] varchar(32) NOT NULL,
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
        CONSTRAINT [CK_CounterTransactions_Total_NonNegative] CHECK (Total >= 0),
        CONSTRAINT [FK_CounterTransactions_Branches_BranchCode] FOREIGN KEY ([BranchCode]) REFERENCES [Branches] ([Code]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CounterTransactions_Staff_ClerkId] FOREIGN KEY ([ClerkId]) REFERENCES [Staff] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE TABLE [IdentityVerifications] (
        [Id] uniqueidentifier NOT NULL,
        [StaffId] uniqueidentifier NOT NULL,
        [TransactionId] uniqueidentifier NULL,
        [BranchCode] varchar(32) NOT NULL,
        [DocumentType] nvarchar(64) NOT NULL,
        [IsVerified] bit NOT NULL DEFAULT CAST(0 AS bit),
        [MaskedLast4] varchar(4) NOT NULL,
        [VerifiedAtUtc] datetimeoffset NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(128) NULL DEFAULT N'system',
        [ModifiedAtUtc] datetimeoffset NULL,
        [ModifiedBy] nvarchar(128) NULL,
        CONSTRAINT [PK_IdentityVerifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_IdentityVerifications_Branches_BranchCode] FOREIGN KEY ([BranchCode]) REFERENCES [Branches] ([Code]) ON DELETE NO ACTION,
        CONSTRAINT [FK_IdentityVerifications_CounterTransactions_TransactionId] FOREIGN KEY ([TransactionId]) REFERENCES [CounterTransactions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_IdentityVerifications_Staff_StaffId] FOREIGN KEY ([StaffId]) REFERENCES [Staff] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE TABLE [MoneyOrders] (
        [Id] uniqueidentifier NOT NULL,
        [TransactionId] uniqueidentifier NULL,
        [BranchCode] varchar(32) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Sender] nvarchar(200) NOT NULL,
        [Recipient] nvarchar(200) NOT NULL,
        [Fee] decimal(18,2) NOT NULL,
        [RequiresIdentityVerification] bit NOT NULL DEFAULT CAST(0 AS bit),
        [IdentityVerificationComplete] bit NOT NULL DEFAULT CAST(0 AS bit),
        [Status] varchar(32) NOT NULL DEFAULT 'Pending',
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [ModifiedAtUtc] datetimeoffset NULL,
        [CreatedBy] nvarchar(128) NULL DEFAULT N'system',
        [ModifiedBy] nvarchar(128) NULL,
        CONSTRAINT [PK_MoneyOrders] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_MoneyOrders_Amount_Positive] CHECK (Amount > 0),
        CONSTRAINT [CK_MoneyOrders_Fee_NonNegative] CHECK (Fee >= 0),
        CONSTRAINT [CK_MoneyOrders_Status_Valid] CHECK (Status IN ('Pending','Approved','Rejected')),
        CONSTRAINT [FK_MoneyOrders_Branches_BranchCode] FOREIGN KEY ([BranchCode]) REFERENCES [Branches] ([Code]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MoneyOrders_CounterTransactions_TransactionId] FOREIGN KEY ([TransactionId]) REFERENCES [CounterTransactions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE TABLE [Payments] (
        [Id] uniqueidentifier NOT NULL,
        [CounterTransactionId] uniqueidentifier NOT NULL,
        [Method] varchar(32) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Status] varchar(32) NOT NULL DEFAULT 'Captured',
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(128) NULL DEFAULT N'system',
        [ModifiedAtUtc] datetimeoffset NULL,
        [ModifiedBy] nvarchar(128) NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Payments_Amount_Positive] CHECK (Amount > 0),
        CONSTRAINT [CK_Payments_Status_Valid] CHECK (Status IN ('Captured','Refunded','Failed')),
        CONSTRAINT [FK_Payments_CounterTransactions_CounterTransactionId] FOREIGN KEY ([CounterTransactionId]) REFERENCES [CounterTransactions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE TABLE [Shipments] (
        [Id] uniqueidentifier NOT NULL,
        [CounterTransactionId] uniqueidentifier NOT NULL,
        [BranchCode] varchar(32) NOT NULL,
        [ZoneId] int NOT NULL,
        [ServiceLevel] varchar(32) NOT NULL,
        [WeightKg] decimal(8,2) NOT NULL,
        [Price] decimal(18,2) NOT NULL,
        [TrackingReference] varchar(40) NOT NULL,
        [Status] varchar(32) NOT NULL DEFAULT 'Created',
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(128) NULL DEFAULT N'system',
        [ModifiedAtUtc] datetimeoffset NULL,
        [ModifiedBy] nvarchar(128) NULL,
        CONSTRAINT [PK_Shipments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Shipments_Price_NonNegative] CHECK (Price >= 0),
        CONSTRAINT [CK_Shipments_Status_Valid] CHECK (Status IN ('Created','InTransit','Delivered','Returned')),
        CONSTRAINT [CK_Shipments_Weight_Range] CHECK (WeightKg > 0 AND WeightKg <= 30),
        CONSTRAINT [FK_Shipments_Branches_BranchCode] FOREIGN KEY ([BranchCode]) REFERENCES [Branches] ([Code]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Shipments_CounterTransactions_CounterTransactionId] FOREIGN KEY ([CounterTransactionId]) REFERENCES [CounterTransactions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Shipments_ShippingZones_ZoneId] FOREIGN KEY ([ZoneId]) REFERENCES [ShippingZones] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE TABLE [TransactionLines] (
        [Id] uniqueidentifier NOT NULL,
        [Description] nvarchar(200) NOT NULL,
        [ProductSku] varchar(64) NULL,
        [ServiceCode] varchar(64) NULL,
        [Quantity] int NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [TaxCategory] varchar(32) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [ModifiedAtUtc] datetimeoffset NULL,
        [CounterTransactionId] uniqueidentifier NOT NULL,
        [CreatedBy] nvarchar(128) NULL DEFAULT N'system',
        [ModifiedBy] nvarchar(128) NULL,
        CONSTRAINT [PK_TransactionLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_TransactionLines_Quantity_Positive] CHECK (Quantity > 0),
        CONSTRAINT [CK_TransactionLines_RetailXorService] CHECK ((ProductSku IS NOT NULL AND ServiceCode IS NULL) OR (ProductSku IS NULL AND ServiceCode IS NOT NULL)),
        CONSTRAINT [CK_TransactionLines_UnitPrice_NonNegative] CHECK (UnitPrice >= 0),
        CONSTRAINT [FK_TransactionLines_CounterTransactions_CounterTransactionId] FOREIGN KEY ([CounterTransactionId]) REFERENCES [CounterTransactions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'DisplayName') AND [object_id] = OBJECT_ID(N'[ProductCategories]'))
        SET IDENTITY_INSERT [ProductCategories] ON;
    EXEC(N'INSERT INTO [ProductCategories] ([Id], [DisplayName])
    VALUES (''OfficeSupplies'', N''OfficeSupplies''),
    (''Packaging'', N''Packaging''),
    (''Stationery'', N''Stationery'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'DisplayName') AND [object_id] = OBJECT_ID(N'[ProductCategories]'))
        SET IDENTITY_INSERT [ProductCategories] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'DisplayName') AND [object_id] = OBJECT_ID(N'[Roles]'))
        SET IDENTITY_INSERT [Roles] ON;
    EXEC(N'INSERT INTO [Roles] ([Id], [DisplayName])
    VALUES (''Administrator'', N''Administrator''),
    (''Auditor'', N''Auditor''),
    (''BranchManager'', N''BranchManager''),
    (''CounterClerk'', N''CounterClerk'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'DisplayName') AND [object_id] = OBJECT_ID(N'[Roles]'))
        SET IDENTITY_INSERT [Roles] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BranchStock_BranchCode_ProductSku] ON [BranchStock] ([BranchCode], [ProductSku]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_BranchStock_ProductSku] ON [BranchStock] ([ProductSku]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Counters_BranchCode_CounterCode] ON [Counters] ([BranchCode], [CounterCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CounterTransactions_BranchCode] ON [CounterTransactions] ([BranchCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CounterTransactions_ClerkId] ON [CounterTransactions] ([ClerkId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_IdentityVerifications_BranchCode] ON [IdentityVerifications] ([BranchCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_IdentityVerifications_StaffId] ON [IdentityVerifications] ([StaffId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_IdentityVerifications_TransactionId] ON [IdentityVerifications] ([TransactionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MoneyOrders_BranchCode] ON [MoneyOrders] ([BranchCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MoneyOrders_TransactionId] ON [MoneyOrders] ([TransactionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Payments_CounterTransactionId] ON [Payments] ([CounterTransactionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Products_Category] ON [Products] ([Category]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Shipments_BranchCode] ON [Shipments] ([BranchCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Shipments_CounterTransactionId] ON [Shipments] ([CounterTransactionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Shipments_TrackingReference] ON [Shipments] ([TrackingReference]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Shipments_ZoneId] ON [Shipments] ([ZoneId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ShippingRates_ZoneId_ServiceLevel_MaxWeightKg] ON [ShippingRates] ([ZoneId], [ServiceLevel], [MaxWeightKg]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ShippingZones_Code] ON [ShippingZones] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Staff_Email] ON [Staff] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Staff_HomeBranch] ON [Staff] ([HomeBranch]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Staff_Role] ON [Staff] ([Role]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_StockAdjustments_BranchCode] ON [StockAdjustments] ([BranchCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_StockAdjustments_ProductSku] ON [StockAdjustments] ([ProductSku]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TransactionLines_CounterTransactionId] ON [TransactionLines] ([CounterTransactionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100244_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261102100244_InitialCreate', N'10.0.0');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100256_AddReportingIndexes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261102100256_AddReportingIndexes', N'10.0.0');
END;

COMMIT;
GO

