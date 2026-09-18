-- ================================================================================
-- TMPMS - CSDL HỆ THỐNG QUẢN LÝ NHÀ THUỐC & PHÒNG KHÁM ĐÔNG Y (SCHEMA ĐẦY ĐỦ)
-- Tự động tạo CSDL TMPMS_DB nếu chưa có và chuyển ngữ cảnh sang TMPMS_DB
-- ================================================================================
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'TMPMS_DB')
BEGIN
    CREATE DATABASE TMPMS_DB;
END;
GO

USE TMPMS_DB;
GO

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
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] int NOT NULL IDENTITY,
        [Description] nvarchar(max) NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] int NOT NULL IDENTITY,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
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
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE TABLE [Categories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE TABLE [Suppliers] (
        [Id] int NOT NULL IDENTITY,
        [CompanyName] nvarchar(max) NOT NULL,
        [ContactPerson] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [Phone] nvarchar(max) NOT NULL,
        [Address] nvarchar(max) NOT NULL,
        [TaxCode] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Suppliers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE TABLE [Warehouses] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Address] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Warehouses] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] int NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] int NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] int NOT NULL,
        [RoleId] int NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] int NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE TABLE [Carts] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        CONSTRAINT [PK_Carts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Carts_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE TABLE [Diagnoses] (
        [Id] int NOT NULL IDENTITY,
        [PatientId] int NOT NULL,
        [DoctorId] int NOT NULL,
        [Symptoms] nvarchar(max) NOT NULL,
        [ClinicalExamination] nvarchar(max) NOT NULL,
        [DiagnosisResult] nvarchar(max) NOT NULL,
        [Note] nvarchar(max) NOT NULL,
        [DiagnosisDate] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Diagnoses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Diagnoses_AspNetUsers_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Diagnoses_AspNetUsers_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE TABLE [Orders] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [ShippingAddress] nvarchar(max) NOT NULL,
        [PaymentStatus] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Orders_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE TABLE [RefreshTokens] (
        [Id] int NOT NULL IDENTITY,
        [Token] nvarchar(max) NOT NULL,
        [UserId] int NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RevokedAt] datetime2 NULL,
        [ReplacedByToken] nvarchar(max) NULL,
        [CreatedByIp] nvarchar(max) NULL,
        [RevokedByIp] nvarchar(max) NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RefreshTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE TABLE [UserAddresses] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [AddressLine] nvarchar(max) NOT NULL,
        [City] nvarchar(max) NOT NULL,
        [District] nvarchar(max) NOT NULL,
        [Ward] nvarchar(max) NOT NULL,
        [IsDefault] bit NOT NULL,
        CONSTRAINT [PK_UserAddresses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserAddresses_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE TABLE [Medicines] (
        [Id] int NOT NULL IDENTITY,
        [CategoryId] int NOT NULL,
        [SupplierId] int NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Price] decimal(18,2) NOT NULL,
        [StockQuantity] int NOT NULL,
        [ManufactureDate] datetime2 NOT NULL,
        [ExpiryDate] datetime2 NOT NULL,
        [RequiresPrescription] bit NOT NULL,
        [ImageUrl] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Medicines] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Medicines_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Medicines_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE TABLE [Prescriptions] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [DiagnosisId] int NULL,
        [DoctorId] int NULL,
        [DoctorName] nvarchar(max) NOT NULL,
        [Hospital] nvarchar(max) NOT NULL,
        [PrescriptionDate] datetime2 NOT NULL,
        [ImageUrl] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Prescriptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Prescriptions_AspNetUsers_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Prescriptions_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Prescriptions_Diagnoses_DiagnosisId] FOREIGN KEY ([DiagnosisId]) REFERENCES [Diagnoses] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE TABLE [Invoices] (
        [Id] int NOT NULL IDENTITY,
        [OrderId] int NOT NULL,
        [InvoiceCode] nvarchar(max) NOT NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        [IssuedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Invoices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Invoices_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE TABLE [Payments] (
        [Id] int NOT NULL IDENTITY,
        [OrderId] int NOT NULL,
        [Method] nvarchar(max) NOT NULL,
        [TransactionCode] nvarchar(max) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [PaidAt] datetime2 NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Payments_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE TABLE [CartItems] (
        [Id] int NOT NULL IDENTITY,
        [CartId] int NOT NULL,
        [MedicineId] int NOT NULL,
        [Quantity] int NOT NULL,
        CONSTRAINT [PK_CartItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CartItems_Carts_CartId] FOREIGN KEY ([CartId]) REFERENCES [Carts] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CartItems_Medicines_MedicineId] FOREIGN KEY ([MedicineId]) REFERENCES [Medicines] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE TABLE [HerbalMedicineInfos] (
        [Id] int NOT NULL IDENTITY,
        [MedicineId] int NOT NULL,
        [OriginPlace] nvarchar(max) NOT NULL,
        [PartUsed] nvarchar(max) NOT NULL,
        [Properties] nvarchar(max) NOT NULL,
        [Effects] nvarchar(max) NOT NULL,
        [UsageInstructions] nvarchar(max) NOT NULL,
        [Dosage] nvarchar(max) NOT NULL,
        [Contraindications] nvarchar(max) NOT NULL,
        [PreservationMethod] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_HerbalMedicineInfos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HerbalMedicineInfos_Medicines_MedicineId] FOREIGN KEY ([MedicineId]) REFERENCES [Medicines] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE TABLE [InventoryStocks] (
        [MedicineId] int NOT NULL,
        [WarehouseId] int NOT NULL,
        [Quantity] int NOT NULL,
        CONSTRAINT [PK_InventoryStocks] PRIMARY KEY ([MedicineId], [WarehouseId]),
        CONSTRAINT [FK_InventoryStocks_Medicines_MedicineId] FOREIGN KEY ([MedicineId]) REFERENCES [Medicines] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_InventoryStocks_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE TABLE [InventoryTransactions] (
        [Id] int NOT NULL IDENTITY,
        [MedicineId] int NOT NULL,
        [WarehouseId] int NOT NULL,
        [Type] nvarchar(max) NOT NULL,
        [Quantity] int NOT NULL,
        [ReferenceId] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_InventoryTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InventoryTransactions_Medicines_MedicineId] FOREIGN KEY ([MedicineId]) REFERENCES [Medicines] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_InventoryTransactions_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE TABLE [MedicineImages] (
        [Id] int NOT NULL IDENTITY,
        [MedicineId] int NOT NULL,
        [ImageUrl] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_MedicineImages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MedicineImages_Medicines_MedicineId] FOREIGN KEY ([MedicineId]) REFERENCES [Medicines] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE TABLE [OrderItems] (
        [Id] int NOT NULL IDENTITY,
        [OrderId] int NOT NULL,
        [MedicineId] int NOT NULL,
        [Quantity] int NOT NULL,
        [Price] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrderItems_Medicines_MedicineId] FOREIGN KEY ([MedicineId]) REFERENCES [Medicines] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE TABLE [Reviews] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [MedicineId] int NOT NULL,
        [Rating] int NOT NULL,
        [Comment] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Reviews] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Reviews_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Reviews_Medicines_MedicineId] FOREIGN KEY ([MedicineId]) REFERENCES [Medicines] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE TABLE [SupplierMedicines] (
        [SupplierId] int NOT NULL,
        [MedicineId] int NOT NULL,
        CONSTRAINT [PK_SupplierMedicines] PRIMARY KEY ([SupplierId], [MedicineId]),
        CONSTRAINT [FK_SupplierMedicines_Medicines_MedicineId] FOREIGN KEY ([MedicineId]) REFERENCES [Medicines] ([Id]),
        CONSTRAINT [FK_SupplierMedicines_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE TABLE [PrescriptionItems] (
        [Id] int NOT NULL IDENTITY,
        [PrescriptionId] int NOT NULL,
        [MedicineId] int NOT NULL,
        [Quantity] int NOT NULL,
        CONSTRAINT [PK_PrescriptionItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PrescriptionItems_Medicines_MedicineId] FOREIGN KEY ([MedicineId]) REFERENCES [Medicines] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PrescriptionItems_Prescriptions_PrescriptionId] FOREIGN KEY ([PrescriptionId]) REFERENCES [Prescriptions] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_CartItems_CartId] ON [CartItems] ([CartId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_CartItems_MedicineId] ON [CartItems] ([MedicineId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_Carts_UserId] ON [Carts] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_Diagnoses_DoctorId] ON [Diagnoses] ([DoctorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_Diagnoses_PatientId] ON [Diagnoses] ([PatientId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE UNIQUE INDEX [IX_HerbalMedicineInfos_MedicineId] ON [HerbalMedicineInfos] ([MedicineId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_InventoryStocks_WarehouseId] ON [InventoryStocks] ([WarehouseId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_InventoryTransactions_MedicineId] ON [InventoryTransactions] ([MedicineId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_InventoryTransactions_WarehouseId] ON [InventoryTransactions] ([WarehouseId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_Invoices_OrderId] ON [Invoices] ([OrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_MedicineImages_MedicineId] ON [MedicineImages] ([MedicineId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_Medicines_CategoryId] ON [Medicines] ([CategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_Medicines_SupplierId] ON [Medicines] ([SupplierId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_OrderItems_MedicineId] ON [OrderItems] ([MedicineId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_OrderItems_OrderId] ON [OrderItems] ([OrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_Orders_UserId] ON [Orders] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_Payments_OrderId] ON [Payments] ([OrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_PrescriptionItems_MedicineId] ON [PrescriptionItems] ([MedicineId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_PrescriptionItems_PrescriptionId] ON [PrescriptionItems] ([PrescriptionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_Prescriptions_DiagnosisId] ON [Prescriptions] ([DiagnosisId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_Prescriptions_DoctorId] ON [Prescriptions] ([DoctorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_Prescriptions_UserId] ON [Prescriptions] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_Reviews_MedicineId] ON [Reviews] ([MedicineId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_Reviews_UserId] ON [Reviews] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_SupplierMedicines_MedicineId] ON [SupplierMedicines] ([MedicineId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_UserAddresses_UserId] ON [UserAddresses] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705205931_AddIdentity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260705205931_AddIdentity', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706181103_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260706181103_InitialCreate', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723191907_Add-Appointment'
)
BEGIN
    CREATE TABLE [Appointments] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [StaffId] int NULL,
        [AppointmentDate] datetime2 NOT NULL,
        [Reason] nvarchar(max) NOT NULL,
        [Note] nvarchar(max) NULL,
        [Status] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Appointments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Appointments_AspNetUsers_StaffId] FOREIGN KEY ([StaffId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_Appointments_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723191907_Add-Appointment'
)
BEGIN
    CREATE INDEX [IX_Appointments_StaffId] ON [Appointments] ([StaffId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723191907_Add-Appointment'
)
BEGIN
    CREATE INDEX [IX_Appointments_UserId] ON [Appointments] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723191907_Add-Appointment'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260723191907_Add-Appointment', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729125924_AddUserProfileAndVoucherFields'
)
BEGIN
    ALTER TABLE [Orders] ADD [DeliveryMethod] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729125924_AddUserProfileAndVoucherFields'
)
BEGIN
    ALTER TABLE [Orders] ADD [ShippingFee] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729125924_AddUserProfileAndVoucherFields'
)
BEGIN
    ALTER TABLE [Medicines] ADD [Discount] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729125924_AddUserProfileAndVoucherFields'
)
BEGIN
    ALTER TABLE [Medicines] ADD [OldPrice] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729125924_AddUserProfileAndVoucherFields'
)
BEGIN
    ALTER TABLE [Medicines] ADD [Origin] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729125924_AddUserProfileAndVoucherFields'
)
BEGIN
    ALTER TABLE [Medicines] ADD [Packaging] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729125924_AddUserProfileAndVoucherFields'
)
BEGIN
    ALTER TABLE [Medicines] ADD [Unit] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729125924_AddUserProfileAndVoucherFields'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [Address] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729125924_AddUserProfileAndVoucherFields'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [AvatarUrl] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729125924_AddUserProfileAndVoucherFields'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [DateOfBirth] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729125924_AddUserProfileAndVoucherFields'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [FullName] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729125924_AddUserProfileAndVoucherFields'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [Gender] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729125924_AddUserProfileAndVoucherFields'
)
BEGIN
    CREATE TABLE [Vouchers] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(max) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [DiscountType] nvarchar(max) NOT NULL,
        [DiscountValue] decimal(18,2) NOT NULL,
        [MinOrderValue] decimal(18,2) NOT NULL,
        [MaxDiscount] decimal(18,2) NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NULL,
        [UsageLimit] int NOT NULL,
        [UsedCount] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Vouchers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729125924_AddUserProfileAndVoucherFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260729125924_AddUserProfileAndVoucherFields', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730093437_AddSymptomDiagnosisSystem'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Diagnoses]') AND [c].[name] = N'DoctorId');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Diagnoses] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [Diagnoses] ALTER COLUMN [DoctorId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730093437_AddSymptomDiagnosisSystem'
)
BEGIN
    ALTER TABLE [Diagnoses] ADD [PrimarySyndromeId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730093437_AddSymptomDiagnosisSystem'
)
BEGIN
    ALTER TABLE [Diagnoses] ADD [ScoreSnapshotJson] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730093437_AddSymptomDiagnosisSystem'
)
BEGIN
    ALTER TABLE [Diagnoses] ADD [SecondarySyndromeId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730093437_AddSymptomDiagnosisSystem'
)
BEGIN
    CREATE TABLE [SymptomQuestions] (
        [Id] int NOT NULL IDENTITY,
        [QuestionText] nvarchar(max) NOT NULL,
        [QuestionOrder] int NOT NULL,
        [Category] nvarchar(max) NULL,
        CONSTRAINT [PK_SymptomQuestions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730093437_AddSymptomDiagnosisSystem'
)
BEGIN
    CREATE TABLE [SyndromeTypes] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(max) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [RecommendationText] nvarchar(max) NULL,
        CONSTRAINT [PK_SyndromeTypes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730093437_AddSymptomDiagnosisSystem'
)
BEGIN
    CREATE TABLE [AnswerOptions] (
        [Id] int NOT NULL IDENTITY,
        [QuestionId] int NOT NULL,
        [OptionText] nvarchar(max) NOT NULL,
        [OptionOrder] int NOT NULL,
        CONSTRAINT [PK_AnswerOptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AnswerOptions_SymptomQuestions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [SymptomQuestions] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730093437_AddSymptomDiagnosisSystem'
)
BEGIN
    CREATE TABLE [AnswerScoreMappings] (
        [Id] int NOT NULL IDENTITY,
        [AnswerOptionId] int NOT NULL,
        [SyndromeTypeId] int NOT NULL,
        [Points] int NOT NULL,
        CONSTRAINT [PK_AnswerScoreMappings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AnswerScoreMappings_AnswerOptions_AnswerOptionId] FOREIGN KEY ([AnswerOptionId]) REFERENCES [AnswerOptions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AnswerScoreMappings_SyndromeTypes_SyndromeTypeId] FOREIGN KEY ([SyndromeTypeId]) REFERENCES [SyndromeTypes] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730093437_AddSymptomDiagnosisSystem'
)
BEGIN
    CREATE TABLE [DiagnosisAnswers] (
        [Id] int NOT NULL IDENTITY,
        [DiagnosisId] int NOT NULL,
        [QuestionId] int NOT NULL,
        [AnswerOptionId] int NOT NULL,
        CONSTRAINT [PK_DiagnosisAnswers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DiagnosisAnswers_AnswerOptions_AnswerOptionId] FOREIGN KEY ([AnswerOptionId]) REFERENCES [AnswerOptions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DiagnosisAnswers_Diagnoses_DiagnosisId] FOREIGN KEY ([DiagnosisId]) REFERENCES [Diagnoses] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DiagnosisAnswers_SymptomQuestions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [SymptomQuestions] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730093437_AddSymptomDiagnosisSystem'
)
BEGIN
    CREATE INDEX [IX_Diagnoses_PrimarySyndromeId] ON [Diagnoses] ([PrimarySyndromeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730093437_AddSymptomDiagnosisSystem'
)
BEGIN
    CREATE INDEX [IX_Diagnoses_SecondarySyndromeId] ON [Diagnoses] ([SecondarySyndromeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730093437_AddSymptomDiagnosisSystem'
)
BEGIN
    CREATE INDEX [IX_AnswerOptions_QuestionId] ON [AnswerOptions] ([QuestionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730093437_AddSymptomDiagnosisSystem'
)
BEGIN
    CREATE INDEX [IX_AnswerScoreMappings_AnswerOptionId] ON [AnswerScoreMappings] ([AnswerOptionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730093437_AddSymptomDiagnosisSystem'
)
BEGIN
    CREATE INDEX [IX_AnswerScoreMappings_SyndromeTypeId] ON [AnswerScoreMappings] ([SyndromeTypeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730093437_AddSymptomDiagnosisSystem'
)
BEGIN
    CREATE INDEX [IX_DiagnosisAnswers_AnswerOptionId] ON [DiagnosisAnswers] ([AnswerOptionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730093437_AddSymptomDiagnosisSystem'
)
BEGIN
    CREATE INDEX [IX_DiagnosisAnswers_DiagnosisId] ON [DiagnosisAnswers] ([DiagnosisId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730093437_AddSymptomDiagnosisSystem'
)
BEGIN
    CREATE INDEX [IX_DiagnosisAnswers_QuestionId] ON [DiagnosisAnswers] ([QuestionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730093437_AddSymptomDiagnosisSystem'
)
BEGIN
    ALTER TABLE [Diagnoses] ADD CONSTRAINT [FK_Diagnoses_SyndromeTypes_PrimarySyndromeId] FOREIGN KEY ([PrimarySyndromeId]) REFERENCES [SyndromeTypes] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730093437_AddSymptomDiagnosisSystem'
)
BEGIN
    ALTER TABLE [Diagnoses] ADD CONSTRAINT [FK_Diagnoses_SyndromeTypes_SecondarySyndromeId] FOREIGN KEY ([SecondarySyndromeId]) REFERENCES [SyndromeTypes] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730093437_AddSymptomDiagnosisSystem'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260730093437_AddSymptomDiagnosisSystem', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731120703_AddPharmacyLiveChat'
)
BEGIN
    CREATE TABLE [PharmacyChatSessions] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [AssignedPharmacistId] int NULL,
        [Status] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [LastMessageAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PharmacyChatSessions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PharmacyChatSessions_AspNetUsers_AssignedPharmacistId] FOREIGN KEY ([AssignedPharmacistId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PharmacyChatSessions_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731120703_AddPharmacyLiveChat'
)
BEGIN
    CREATE TABLE [PharmacyChatMessages] (
        [Id] int NOT NULL IDENTITY,
        [SessionId] int NOT NULL,
        [SenderId] int NOT NULL,
        [SenderRole] nvarchar(max) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [SentAt] datetime2 NOT NULL,
        [IsRead] bit NOT NULL,
        CONSTRAINT [PK_PharmacyChatMessages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PharmacyChatMessages_AspNetUsers_SenderId] FOREIGN KEY ([SenderId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PharmacyChatMessages_PharmacyChatSessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [PharmacyChatSessions] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731120703_AddPharmacyLiveChat'
)
BEGIN
    CREATE INDEX [IX_PharmacyChatMessages_SenderId] ON [PharmacyChatMessages] ([SenderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731120703_AddPharmacyLiveChat'
)
BEGIN
    CREATE INDEX [IX_PharmacyChatMessages_SessionId] ON [PharmacyChatMessages] ([SessionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731120703_AddPharmacyLiveChat'
)
BEGIN
    CREATE INDEX [IX_PharmacyChatSessions_AssignedPharmacistId] ON [PharmacyChatSessions] ([AssignedPharmacistId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731120703_AddPharmacyLiveChat'
)
BEGIN
    CREATE INDEX [IX_PharmacyChatSessions_UserId] ON [PharmacyChatSessions] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731120703_AddPharmacyLiveChat'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260731120703_AddPharmacyLiveChat', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801011220_AddPharmacyStoresTable'
)
BEGIN
    CREATE TABLE [Stores] (
        [Id] int NOT NULL IDENTITY,
        [ProvinceId] nvarchar(max) NOT NULL,
        [ProvinceName] nvarchar(max) NOT NULL,
        [DistrictId] nvarchar(max) NOT NULL,
        [DistrictName] nvarchar(max) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Address] nvarchar(max) NOT NULL,
        [Latitude] float NOT NULL,
        [Longitude] float NOT NULL,
        [Phone] nvarchar(max) NOT NULL,
        [Hours] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Stores] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801011220_AddPharmacyStoresTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260801011220_AddPharmacyStoresTable', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801013156_AddHealthQuizEngine'
)
BEGIN
    CREATE TABLE [HealthQuizzes] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(450) NOT NULL,
        [Title] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [IconUrl] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_HealthQuizzes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801013156_AddHealthQuizEngine'
)
BEGIN
    CREATE TABLE [QuizQuestions] (
        [Id] int NOT NULL IDENTITY,
        [QuizId] int NOT NULL,
        [QuestionText] nvarchar(max) NOT NULL,
        [QuestionOrder] int NOT NULL,
        CONSTRAINT [PK_QuizQuestions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QuizQuestions_HealthQuizzes_QuizId] FOREIGN KEY ([QuizId]) REFERENCES [HealthQuizzes] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801013156_AddHealthQuizEngine'
)
BEGIN
    CREATE TABLE [QuizResultBands] (
        [Id] int NOT NULL IDENTITY,
        [QuizId] int NOT NULL,
        [MinScore] int NOT NULL,
        [MaxScore] int NOT NULL,
        [Label] nvarchar(max) NOT NULL,
        [RiskLevel] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [RecommendationText] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_QuizResultBands] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QuizResultBands_HealthQuizzes_QuizId] FOREIGN KEY ([QuizId]) REFERENCES [HealthQuizzes] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801013156_AddHealthQuizEngine'
)
BEGIN
    CREATE TABLE [QuizAnswerOptions] (
        [Id] int NOT NULL IDENTITY,
        [QuestionId] int NOT NULL,
        [OptionText] nvarchar(max) NOT NULL,
        [OptionOrder] int NOT NULL,
        [Points] int NOT NULL,
        CONSTRAINT [PK_QuizAnswerOptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QuizAnswerOptions_QuizQuestions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [QuizQuestions] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801013156_AddHealthQuizEngine'
)
BEGIN
    CREATE TABLE [QuizSessions] (
        [Id] int NOT NULL IDENTITY,
        [QuizId] int NOT NULL,
        [UserId] int NULL,
        [TotalScore] int NOT NULL,
        [ResultBandId] int NULL,
        [ScoreSnapshotJson] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_QuizSessions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QuizSessions_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_QuizSessions_HealthQuizzes_QuizId] FOREIGN KEY ([QuizId]) REFERENCES [HealthQuizzes] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_QuizSessions_QuizResultBands_ResultBandId] FOREIGN KEY ([ResultBandId]) REFERENCES [QuizResultBands] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801013156_AddHealthQuizEngine'
)
BEGIN
    CREATE UNIQUE INDEX [IX_HealthQuizzes_Code] ON [HealthQuizzes] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801013156_AddHealthQuizEngine'
)
BEGIN
    CREATE INDEX [IX_QuizAnswerOptions_QuestionId] ON [QuizAnswerOptions] ([QuestionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801013156_AddHealthQuizEngine'
)
BEGIN
    CREATE INDEX [IX_QuizQuestions_QuizId] ON [QuizQuestions] ([QuizId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801013156_AddHealthQuizEngine'
)
BEGIN
    CREATE INDEX [IX_QuizResultBands_QuizId] ON [QuizResultBands] ([QuizId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801013156_AddHealthQuizEngine'
)
BEGIN
    CREATE INDEX [IX_QuizSessions_QuizId] ON [QuizSessions] ([QuizId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801013156_AddHealthQuizEngine'
)
BEGIN
    CREATE INDEX [IX_QuizSessions_ResultBandId] ON [QuizSessions] ([ResultBandId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801013156_AddHealthQuizEngine'
)
BEGIN
    CREATE INDEX [IX_QuizSessions_UserId] ON [QuizSessions] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801013156_AddHealthQuizEngine'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260801013156_AddHealthQuizEngine', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801121011_LinkHerbalMedicineToMedicine'
)
BEGIN
    ALTER TABLE [HerbalMedicineInfos] DROP CONSTRAINT [FK_HerbalMedicineInfos_Medicines_MedicineId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801121011_LinkHerbalMedicineToMedicine'
)
BEGIN
    DROP INDEX [IX_HerbalMedicineInfos_MedicineId] ON [HerbalMedicineInfos];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801121011_LinkHerbalMedicineToMedicine'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Medicines]') AND [c].[name] = N'Price');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Medicines] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [Medicines] ALTER COLUMN [Price] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801121011_LinkHerbalMedicineToMedicine'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[HerbalMedicineInfos]') AND [c].[name] = N'MedicineId');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [HerbalMedicineInfos] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [HerbalMedicineInfos] ALTER COLUMN [MedicineId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801121011_LinkHerbalMedicineToMedicine'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_HerbalMedicineInfos_MedicineId] ON [HerbalMedicineInfos] ([MedicineId]) WHERE [MedicineId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801121011_LinkHerbalMedicineToMedicine'
)
BEGIN
    ALTER TABLE [HerbalMedicineInfos] ADD CONSTRAINT [FK_HerbalMedicineInfos_Medicines_MedicineId] FOREIGN KEY ([MedicineId]) REFERENCES [Medicines] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801121011_LinkHerbalMedicineToMedicine'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260801121011_LinkHerbalMedicineToMedicine', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801131628_AddDiagnosisNoteToPrescription'
)
BEGIN
    ALTER TABLE [Prescriptions] ADD [DiagnosisNote] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801131628_AddDiagnosisNoteToPrescription'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260801131628_AddDiagnosisNoteToPrescription', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801173742_AddMedicineIsActive'
)
BEGIN
    ALTER TABLE [Medicines] ADD [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801173742_AddMedicineIsActive'
)
BEGIN
    UPDATE [Medicines] SET [IsActive] = 1
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801173742_AddMedicineIsActive'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260801173742_AddMedicineIsActive', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801230906_AddAppointmentLinkToPrescription'
)
BEGIN
    ALTER TABLE [Prescriptions] ADD [AppointmentId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801230906_AddAppointmentLinkToPrescription'
)
BEGIN
    CREATE INDEX [IX_Prescriptions_AppointmentId] ON [Prescriptions] ([AppointmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801230906_AddAppointmentLinkToPrescription'
)
BEGIN
    ALTER TABLE [Prescriptions] ADD CONSTRAINT [FK_Prescriptions_Appointments_AppointmentId] FOREIGN KEY ([AppointmentId]) REFERENCES [Appointments] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801230906_AddAppointmentLinkToPrescription'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260801230906_AddAppointmentLinkToPrescription', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802121702_AddOrderReturnReason'
)
BEGIN
    ALTER TABLE [Orders] ADD [ReturnReason] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802121702_AddOrderReturnReason'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260802121702_AddOrderReturnReason', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803000506_AddAppointmentConfirmationFlow'
)
BEGIN
    ALTER TABLE [Appointments] ADD [ConfirmationDeadline] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803000506_AddAppointmentConfirmationFlow'
)
BEGIN
    ALTER TABLE [Appointments] ADD [ConfirmedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803000506_AddAppointmentConfirmationFlow'
)
BEGIN
    ALTER TABLE [Appointments] ADD [ConfirmedByStaffId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803000506_AddAppointmentConfirmationFlow'
)
BEGIN
    ALTER TABLE [Appointments] ADD [RejectionReason] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803000506_AddAppointmentConfirmationFlow'
)
BEGIN
    CREATE INDEX [IX_Appointments_ConfirmedByStaffId] ON [Appointments] ([ConfirmedByStaffId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803000506_AddAppointmentConfirmationFlow'
)
BEGIN
    ALTER TABLE [Appointments] ADD CONSTRAINT [FK_Appointments_AspNetUsers_ConfirmedByStaffId] FOREIGN KEY ([ConfirmedByStaffId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803000506_AddAppointmentConfirmationFlow'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260803000506_AddAppointmentConfirmationFlow', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803210802_AddGoogleLoginSupport'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [GoogleId] nvarchar(450) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803210802_AddGoogleLoginSupport'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_AspNetUsers_GoogleId] ON [AspNetUsers] ([GoogleId]) WHERE [GoogleId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803210802_AddGoogleLoginSupport'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260803210802_AddGoogleLoginSupport', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807021356_AddStockBatchTracking'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Orders]') AND [c].[name] = N'Status');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Orders] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [Orders] ALTER COLUMN [Status] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807021356_AddStockBatchTracking'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Orders]') AND [c].[name] = N'ShippingAddress');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Orders] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [Orders] ALTER COLUMN [ShippingAddress] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807021356_AddStockBatchTracking'
)
BEGIN
    DECLARE @var5 sysname;
    SELECT @var5 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Orders]') AND [c].[name] = N'PaymentStatus');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Orders] DROP CONSTRAINT [' + @var5 + '];');
    ALTER TABLE [Orders] ALTER COLUMN [PaymentStatus] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807021356_AddStockBatchTracking'
)
BEGIN
    ALTER TABLE [InventoryTransactions] ADD [StockBatchId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807021356_AddStockBatchTracking'
)
BEGIN
    CREATE TABLE [StockBatches] (
        [Id] int NOT NULL IDENTITY,
        [MedicineId] int NOT NULL,
        [WarehouseId] int NOT NULL,
        [SupplierId] int NULL,
        [BatchNumber] nvarchar(450) NOT NULL,
        [ManufactureDate] datetime2 NOT NULL,
        [ExpiryDate] datetime2 NOT NULL,
        [QuantityReceived] int NOT NULL,
        [QuantityRemaining] int NOT NULL,
        [UnitCostPrice] decimal(18,2) NULL,
        [ReceivedAt] datetime2 NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [Note] nvarchar(max) NULL,
        CONSTRAINT [PK_StockBatches] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StockBatches_Medicines_MedicineId] FOREIGN KEY ([MedicineId]) REFERENCES [Medicines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockBatches_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockBatches_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807021356_AddStockBatchTracking'
)
BEGIN
    CREATE INDEX [IX_InventoryTransactions_StockBatchId] ON [InventoryTransactions] ([StockBatchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807021356_AddStockBatchTracking'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StockBatches_MedicineId_WarehouseId_BatchNumber] ON [StockBatches] ([MedicineId], [WarehouseId], [BatchNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807021356_AddStockBatchTracking'
)
BEGIN
    CREATE INDEX [IX_StockBatches_MedicineId_WarehouseId_ExpiryDate] ON [StockBatches] ([MedicineId], [WarehouseId], [ExpiryDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807021356_AddStockBatchTracking'
)
BEGIN
    CREATE INDEX [IX_StockBatches_SupplierId] ON [StockBatches] ([SupplierId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807021356_AddStockBatchTracking'
)
BEGIN
    CREATE INDEX [IX_StockBatches_WarehouseId] ON [StockBatches] ([WarehouseId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807021356_AddStockBatchTracking'
)
BEGIN
    ALTER TABLE [InventoryTransactions] ADD CONSTRAINT [FK_InventoryTransactions_StockBatches_StockBatchId] FOREIGN KEY ([StockBatchId]) REFERENCES [StockBatches] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807021356_AddStockBatchTracking'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260807021356_AddStockBatchTracking', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807021447_BackfillInitialStockBatches'
)
BEGIN

    INSERT INTO StockBatches (MedicineId, WarehouseId, SupplierId, BatchNumber, ManufactureDate, ExpiryDate, QuantityReceived, QuantityRemaining, UnitCostPrice, ReceivedAt, Status, Note)
    SELECT m.Id, w.Id, NULL, CONCAT('INIT-', m.Id), m.ManufactureDate, m.ExpiryDate, m.StockQuantity, m.StockQuantity, NULL, GETDATE(), 'Active', N'Lo khoi tao tu dong tu ton kho truoc khi ap dung quan ly theo lo'
    FROM Medicines m
    CROSS JOIN (SELECT TOP 1 Id FROM Warehouses ORDER BY Id) w
    WHERE m.StockQuantity > 0;

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807021447_BackfillInitialStockBatches'
)
BEGIN

    MERGE InventoryStocks AS target
    USING (
        SELECT b.MedicineId, b.WarehouseId, SUM(b.QuantityRemaining) AS Total
        FROM StockBatches b
        WHERE b.Status = 'Active' AND b.ExpiryDate >= GETDATE()
        GROUP BY b.MedicineId, b.WarehouseId
    ) AS source
    ON target.MedicineId = source.MedicineId AND target.WarehouseId = source.WarehouseId
    WHEN MATCHED THEN UPDATE SET target.Quantity = source.Total
    WHEN NOT MATCHED THEN INSERT (MedicineId, WarehouseId, Quantity) VALUES (source.MedicineId, source.WarehouseId, source.Total);

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807021447_BackfillInitialStockBatches'
)
BEGIN

    UPDATE m
    SET m.StockQuantity = ISNULL(agg.Total, 0)
    FROM Medicines m
    LEFT JOIN (
        SELECT MedicineId, SUM(QuantityRemaining) AS Total
        FROM StockBatches
        WHERE Status = 'Active' AND ExpiryDate >= GETDATE()
        GROUP BY MedicineId
    ) agg ON agg.MedicineId = m.Id;

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807021447_BackfillInitialStockBatches'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260807021447_BackfillInitialStockBatches', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807050000_AddAdvancedAppointmentBooking'
)
BEGIN
    ALTER TABLE [Appointments] ADD [CancelledAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807050000_AddAdvancedAppointmentBooking'
)
BEGIN
    ALTER TABLE [Appointments] ADD [CheckedInAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807050000_AddAdvancedAppointmentBooking'
)
BEGIN
    ALTER TABLE [Appointments] ADD [CompletedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807050000_AddAdvancedAppointmentBooking'
)
BEGIN
    ALTER TABLE [Appointments] ADD [DepositAmount] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807050000_AddAdvancedAppointmentBooking'
)
BEGIN
    ALTER TABLE [Appointments] ADD [Location] nvarchar(max) NOT NULL DEFAULT N'Nhà thuốc TMPMS';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807050000_AddAdvancedAppointmentBooking'
)
BEGIN
    ALTER TABLE [Appointments] ADD [PaymentMethod] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807050000_AddAdvancedAppointmentBooking'
)
BEGIN
    ALTER TABLE [Appointments] ADD [PaymentStatus] nvarchar(max) NOT NULL DEFAULT N'Unpaid';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807050000_AddAdvancedAppointmentBooking'
)
BEGIN
    ALTER TABLE [Appointments] ADD [PolicyAcceptedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807050000_AddAdvancedAppointmentBooking'
)
BEGIN
    ALTER TABLE [Appointments] ADD [PrescriptionImageUrl] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807050000_AddAdvancedAppointmentBooking'
)
BEGIN
    ALTER TABLE [Appointments] ADD [ProposedAppointmentDateNote] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807050000_AddAdvancedAppointmentBooking'
)
BEGIN
    ALTER TABLE [Appointments] ADD [RefundAmount] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807050000_AddAdvancedAppointmentBooking'
)
BEGIN
    ALTER TABLE [Appointments] ADD [SymptomDescription] nvarchar(500) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807050000_AddAdvancedAppointmentBooking'
)
BEGIN
    CREATE TABLE [AppointmentSlotHolds] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [StaffId] int NULL,
        [AppointmentDate] datetime2 NOT NULL,
        [Location] nvarchar(450) NOT NULL,
        [Token] nvarchar(450) NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [IsConsumed] bit NOT NULL,
        CONSTRAINT [PK_AppointmentSlotHolds] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AppointmentSlotHolds_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807050000_AddAdvancedAppointmentBooking'
)
BEGIN
    CREATE TABLE [AppointmentPayments] (
        [Id] int NOT NULL IDENTITY,
        [AppointmentId] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Method] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [TransactionCode] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [PaidAt] datetime2 NULL,
        [RefundAmount] decimal(18,2) NOT NULL,
        [RefundStatus] nvarchar(max) NULL,
        CONSTRAINT [PK_AppointmentPayments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AppointmentPayments_Appointments_AppointmentId] FOREIGN KEY ([AppointmentId]) REFERENCES [Appointments] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807050000_AddAdvancedAppointmentBooking'
)
BEGIN
    CREATE TABLE [AppointmentPaymentIntents] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [SlotHoldId] int NOT NULL,
        [OrderCode] bigint NOT NULL,
        [SymptomDescription] nvarchar(max) NOT NULL,
        [PrescriptionImageUrl] nvarchar(max) NULL,
        [Note] nvarchar(max) NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [PaymentLinkId] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AppointmentPaymentIntents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AppointmentPaymentIntents_AppointmentSlotHolds_SlotHoldId] FOREIGN KEY ([SlotHoldId]) REFERENCES [AppointmentSlotHolds] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AppointmentPaymentIntents_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807050000_AddAdvancedAppointmentBooking'
)
BEGIN
    CREATE TABLE [AppointmentRescheduleRequests] (
        [Id] int NOT NULL IDENTITY,
        [AppointmentId] int NOT NULL,
        [OldAppointmentDate] datetime2 NOT NULL,
        [RequestedAppointmentDate] datetime2 NOT NULL,
        [Reason] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ResolvedAt] datetime2 NULL,
        [ResolvedByStaffId] int NULL,
        CONSTRAINT [PK_AppointmentRescheduleRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AppointmentRescheduleRequests_Appointments_AppointmentId] FOREIGN KEY ([AppointmentId]) REFERENCES [Appointments] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807050000_AddAdvancedAppointmentBooking'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_AppointmentSlotHolds_Token] ON [AppointmentSlotHolds] ([Token]) WHERE [Token] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807050000_AddAdvancedAppointmentBooking'
)
BEGIN
    CREATE INDEX [IX_AppointmentSlotHolds_UserId] ON [AppointmentSlotHolds] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807050000_AddAdvancedAppointmentBooking'
)
BEGIN
    CREATE INDEX [IX_AppointmentSlotHolds_AppointmentDate_Location] ON [AppointmentSlotHolds] ([AppointmentDate], [Location]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807050000_AddAdvancedAppointmentBooking'
)
BEGIN
    CREATE INDEX [IX_AppointmentPayments_AppointmentId] ON [AppointmentPayments] ([AppointmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807050000_AddAdvancedAppointmentBooking'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_AppointmentPaymentIntents_OrderCode] ON [AppointmentPaymentIntents] ([OrderCode]) WHERE [OrderCode] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807050000_AddAdvancedAppointmentBooking'
)
BEGIN
    CREATE INDEX [IX_AppointmentPaymentIntents_SlotHoldId] ON [AppointmentPaymentIntents] ([SlotHoldId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807050000_AddAdvancedAppointmentBooking'
)
BEGIN
    CREATE INDEX [IX_AppointmentPaymentIntents_UserId] ON [AppointmentPaymentIntents] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807050000_AddAdvancedAppointmentBooking'
)
BEGIN
    CREATE INDEX [IX_AppointmentRescheduleRequests_AppointmentId] ON [AppointmentRescheduleRequests] ([AppointmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807050000_AddAdvancedAppointmentBooking'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260807050000_AddAdvancedAppointmentBooking', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807072926_AddHerbalInteractions'
)
BEGIN
    CREATE TABLE [HerbalInteractions] (
        [Id] int NOT NULL IDENTITY,
        [HerbAId] int NOT NULL,
        [HerbBId] int NOT NULL,
        [InteractionType] nvarchar(max) NOT NULL,
        [Severity] nvarchar(max) NOT NULL,
        [MechanismDescription] nvarchar(max) NOT NULL,
        [SuggestedReplacementForAId] int NULL,
        [SuggestedReplacementForBId] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_HerbalInteractions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HerbalInteractions_Medicines_HerbAId] FOREIGN KEY ([HerbAId]) REFERENCES [Medicines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_HerbalInteractions_Medicines_HerbBId] FOREIGN KEY ([HerbBId]) REFERENCES [Medicines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_HerbalInteractions_Medicines_SuggestedReplacementForAId] FOREIGN KEY ([SuggestedReplacementForAId]) REFERENCES [Medicines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_HerbalInteractions_Medicines_SuggestedReplacementForBId] FOREIGN KEY ([SuggestedReplacementForBId]) REFERENCES [Medicines] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807072926_AddHerbalInteractions'
)
BEGIN
    CREATE UNIQUE INDEX [IX_HerbalInteractions_HerbAId_HerbBId] ON [HerbalInteractions] ([HerbAId], [HerbBId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807072926_AddHerbalInteractions'
)
BEGIN
    CREATE INDEX [IX_HerbalInteractions_HerbBId] ON [HerbalInteractions] ([HerbBId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807072926_AddHerbalInteractions'
)
BEGIN
    CREATE INDEX [IX_HerbalInteractions_SuggestedReplacementForAId] ON [HerbalInteractions] ([SuggestedReplacementForAId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807072926_AddHerbalInteractions'
)
BEGIN
    CREATE INDEX [IX_HerbalInteractions_SuggestedReplacementForBId] ON [HerbalInteractions] ([SuggestedReplacementForBId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807072926_AddHerbalInteractions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260807072926_AddHerbalInteractions', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807160421_AddOrderProcessedByStaff'
)
BEGIN
    ALTER TABLE [Orders] ADD [ProcessedByStaffId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807160421_AddOrderProcessedByStaff'
)
BEGIN
    CREATE INDEX [IX_Orders_ProcessedByStaffId] ON [Orders] ([ProcessedByStaffId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807160421_AddOrderProcessedByStaff'
)
BEGIN
    ALTER TABLE [Orders] ADD CONSTRAINT [FK_Orders_AspNetUsers_ProcessedByStaffId] FOREIGN KEY ([ProcessedByStaffId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807160421_AddOrderProcessedByStaff'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260807160421_AddOrderProcessedByStaff', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807180004_AddFlashSaleTable'
)
BEGIN
    CREATE TABLE [FlashSales] (
        [Id] int NOT NULL IDENTITY,
        [MedicineId] int NOT NULL,
        [BatchId] int NULL,
        [OriginalPrice] decimal(18,2) NOT NULL,
        [SalePrice] decimal(18,2) NOT NULL,
        [DiscountPercent] int NOT NULL,
        [BatchExpiryDate] datetime2 NULL,
        [DaysUntilExpiryAtApply] int NULL,
        [AppliedAt] datetime2 NOT NULL,
        [AppliedByStaffId] int NULL,
        [IsActive] bit NOT NULL,
        [RemovedAt] datetime2 NULL,
        [RemovedByStaffId] int NULL,
        CONSTRAINT [PK_FlashSales] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FlashSales_AspNetUsers_AppliedByStaffId] FOREIGN KEY ([AppliedByStaffId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_FlashSales_AspNetUsers_RemovedByStaffId] FOREIGN KEY ([RemovedByStaffId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_FlashSales_Medicines_MedicineId] FOREIGN KEY ([MedicineId]) REFERENCES [Medicines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_FlashSales_StockBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [StockBatches] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807180004_AddFlashSaleTable'
)
BEGIN
    CREATE INDEX [IX_FlashSales_AppliedByStaffId] ON [FlashSales] ([AppliedByStaffId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807180004_AddFlashSaleTable'
)
BEGIN
    CREATE INDEX [IX_FlashSales_BatchId] ON [FlashSales] ([BatchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807180004_AddFlashSaleTable'
)
BEGIN
    CREATE INDEX [IX_FlashSales_MedicineId_IsActive] ON [FlashSales] ([MedicineId], [IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807180004_AddFlashSaleTable'
)
BEGIN
    CREATE INDEX [IX_FlashSales_RemovedByStaffId] ON [FlashSales] ([RemovedByStaffId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807180004_AddFlashSaleTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260807180004_AddFlashSaleTable', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807232912_AddVoucherTypeOwnerWheelFields'
)
BEGIN
    DECLARE @var6 sysname;
    SELECT @var6 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Vouchers]') AND [c].[name] = N'Code');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Vouchers] DROP CONSTRAINT [' + @var6 + '];');
    ALTER TABLE [Vouchers] ALTER COLUMN [Code] nvarchar(450) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807232912_AddVoucherTypeOwnerWheelFields'
)
BEGIN
    ALTER TABLE [Vouchers] ADD [IsWheelPrize] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807232912_AddVoucherTypeOwnerWheelFields'
)
BEGIN
    ALTER TABLE [Vouchers] ADD [OwnerUserId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807232912_AddVoucherTypeOwnerWheelFields'
)
BEGIN
    ALTER TABLE [Vouchers] ADD [RowVersion] rowversion NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807232912_AddVoucherTypeOwnerWheelFields'
)
BEGIN
    ALTER TABLE [Vouchers] ADD [Type] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807232912_AddVoucherTypeOwnerWheelFields'
)
BEGIN
    ALTER TABLE [Vouchers] ADD [Weight] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807232912_AddVoucherTypeOwnerWheelFields'
)
BEGIN
    ALTER TABLE [Orders] ADD [ProductVoucherId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807232912_AddVoucherTypeOwnerWheelFields'
)
BEGIN
    ALTER TABLE [Orders] ADD [ShippingVoucherId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807232912_AddVoucherTypeOwnerWheelFields'
)
BEGIN
    CREATE TABLE [WheelSpins] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [SpinDate] datetime2 NOT NULL,
        [VoucherId] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_WheelSpins] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WheelSpins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_WheelSpins_Vouchers_VoucherId] FOREIGN KEY ([VoucherId]) REFERENCES [Vouchers] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807232912_AddVoucherTypeOwnerWheelFields'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Vouchers_Code] ON [Vouchers] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807232912_AddVoucherTypeOwnerWheelFields'
)
BEGIN
    CREATE INDEX [IX_Orders_ProductVoucherId] ON [Orders] ([ProductVoucherId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807232912_AddVoucherTypeOwnerWheelFields'
)
BEGIN
    CREATE INDEX [IX_Orders_ShippingVoucherId] ON [Orders] ([ShippingVoucherId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807232912_AddVoucherTypeOwnerWheelFields'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WheelSpins_UserId_SpinDate] ON [WheelSpins] ([UserId], [SpinDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807232912_AddVoucherTypeOwnerWheelFields'
)
BEGIN
    CREATE INDEX [IX_WheelSpins_VoucherId] ON [WheelSpins] ([VoucherId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807232912_AddVoucherTypeOwnerWheelFields'
)
BEGIN
    ALTER TABLE [Orders] ADD CONSTRAINT [FK_Orders_Vouchers_ProductVoucherId] FOREIGN KEY ([ProductVoucherId]) REFERENCES [Vouchers] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807232912_AddVoucherTypeOwnerWheelFields'
)
BEGIN
    ALTER TABLE [Orders] ADD CONSTRAINT [FK_Orders_Vouchers_ShippingVoucherId] FOREIGN KEY ([ShippingVoucherId]) REFERENCES [Vouchers] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807232912_AddVoucherTypeOwnerWheelFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260807232912_AddVoucherTypeOwnerWheelFields', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807232949_SeedVouchersAndWheelPrizeTemplates'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Code', N'Name', N'DiscountType', N'DiscountValue', N'MinOrderValue', N'MaxDiscount', N'StartDate', N'EndDate', N'UsageLimit', N'UsedCount', N'IsActive', N'CreatedAt', N'Type', N'OwnerUserId', N'IsWheelPrize', N'Weight') AND [object_id] = OBJECT_ID(N'[Vouchers]'))
        SET IDENTITY_INSERT [Vouchers] ON;
    EXEC(N'INSERT INTO [Vouchers] ([Code], [Name], [DiscountType], [DiscountValue], [MinOrderValue], [MaxDiscount], [StartDate], [EndDate], [UsageLimit], [UsedCount], [IsActive], [CreatedAt], [Type], [OwnerUserId], [IsWheelPrize], [Weight])
    VALUES (N''SP10K'', N''Giảm 10.000đ cho đơn từ 100.000đ'', N''flat'', 10000.0, 100000.0, NULL, ''2026-08-08T00:00:00.0000000Z'', NULL, 1000, 0, CAST(1 AS bit), ''2026-08-08T00:00:00.0000000Z'', N''product'', NULL, CAST(0 AS bit), 0),
    (N''SP20K'', N''Giảm 20.000đ cho đơn từ 200.000đ'', N''flat'', 20000.0, 200000.0, NULL, ''2026-08-08T00:00:00.0000000Z'', NULL, 1000, 0, CAST(1 AS bit), ''2026-08-08T00:00:00.0000000Z'', N''product'', NULL, CAST(0 AS bit), 0),
    (N''SP30K'', N''Giảm 30.000đ cho đơn từ 300.000đ'', N''flat'', 30000.0, 300000.0, NULL, ''2026-08-08T00:00:00.0000000Z'', NULL, 1000, 0, CAST(1 AS bit), ''2026-08-08T00:00:00.0000000Z'', N''product'', NULL, CAST(0 AS bit), 0),
    (N''SPSALE'', N''Giảm 5% (tối đa 25.000đ) cho đơn từ 150.000đ'', N''percent'', 5.0, 150000.0, 25000.0, ''2026-08-08T00:00:00.0000000Z'', NULL, 1000, 0, CAST(1 AS bit), ''2026-08-08T00:00:00.0000000Z'', N''product'', NULL, CAST(0 AS bit), 0),
    (N''SHIP10K'', N''Giảm 10.000đ phí vận chuyển'', N''flat'', 10000.0, 0.0, NULL, ''2026-08-08T00:00:00.0000000Z'', NULL, 1000, 0, CAST(1 AS bit), ''2026-08-08T00:00:00.0000000Z'', N''shipping'', NULL, CAST(0 AS bit), 0),
    (N''SHIP20K'', N''Giảm 20.000đ phí vận chuyển cho đơn từ 150.000đ'', N''flat'', 20000.0, 150000.0, NULL, ''2026-08-08T00:00:00.0000000Z'', NULL, 1000, 0, CAST(1 AS bit), ''2026-08-08T00:00:00.0000000Z'', N''shipping'', NULL, CAST(0 AS bit), 0),
    (N''SHIP30K'', N''Giảm 30.000đ phí vận chuyển cho đơn từ 300.000đ'', N''flat'', 30000.0, 300000.0, NULL, ''2026-08-08T00:00:00.0000000Z'', NULL, 1000, 0, CAST(1 AS bit), ''2026-08-08T00:00:00.0000000Z'', N''shipping'', NULL, CAST(0 AS bit), 0),
    (N''WHEEL-TPL-P10'', N''Vòng quay: Giảm 10.000đ sản phẩm'', N''flat'', 10000.0, 0.0, NULL, ''2026-08-08T00:00:00.0000000Z'', NULL, 999999, 0, CAST(1 AS bit), ''2026-08-08T00:00:00.0000000Z'', N''product'', NULL, CAST(1 AS bit), 30),
    (N''WHEEL-TPL-P15'', N''Vòng quay: Giảm 15.000đ sản phẩm'', N''flat'', 15000.0, 0.0, NULL, ''2026-08-08T00:00:00.0000000Z'', NULL, 999999, 0, CAST(1 AS bit), ''2026-08-08T00:00:00.0000000Z'', N''product'', NULL, CAST(1 AS bit), 25),
    (N''WHEEL-TPL-P20'', N''Vòng quay: Giảm 20.000đ sản phẩm'', N''flat'', 20000.0, 0.0, NULL, ''2026-08-08T00:00:00.0000000Z'', NULL, 999999, 0, CAST(1 AS bit), ''2026-08-08T00:00:00.0000000Z'', N''product'', NULL, CAST(1 AS bit), 20),
    (N''WHEEL-TPL-P25'', N''Vòng quay: Giảm 25.000đ sản phẩm'', N''flat'', 25000.0, 0.0, NULL, ''2026-08-08T00:00:00.0000000Z'', NULL, 999999, 0, CAST(1 AS bit), ''2026-08-08T00:00:00.0000000Z'', N''product'', NULL, CAST(1 AS bit), 15),
    (N''WHEEL-TPL-P30'', N''Vòng quay: Giảm 30.000đ sản phẩm'', N''flat'', 30000.0, 0.0, NULL, ''2026-08-08T00:00:00.0000000Z'', NULL, 999999, 0, CAST(1 AS bit), ''2026-08-08T00:00:00.0000000Z'', N''product'', NULL, CAST(1 AS bit), 10),
    (N''WHEEL-TPL-S10'', N''Vòng quay: Giảm 10.000đ phí ship'', N''flat'', 10000.0, 0.0, NULL, ''2026-08-08T00:00:00.0000000Z'', NULL, 999999, 0, CAST(1 AS bit), ''2026-08-08T00:00:00.0000000Z'', N''shipping'', NULL, CAST(1 AS bit), 30),
    (N''WHEEL-TPL-S20'', N''Vòng quay: Giảm 20.000đ phí ship'', N''flat'', 20000.0, 0.0, NULL, ''2026-08-08T00:00:00.0000000Z'', NULL, 999999, 0, CAST(1 AS bit), ''2026-08-08T00:00:00.0000000Z'', N''shipping'', NULL, CAST(1 AS bit), 20),
    (N''WHEEL-TPL-S30'', N''Vòng quay: Giảm 30.000đ phí ship'', N''flat'', 30000.0, 0.0, NULL, ''2026-08-08T00:00:00.0000000Z'', NULL, 999999, 0, CAST(1 AS bit), ''2026-08-08T00:00:00.0000000Z'', N''shipping'', NULL, CAST(1 AS bit), 10)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Code', N'Name', N'DiscountType', N'DiscountValue', N'MinOrderValue', N'MaxDiscount', N'StartDate', N'EndDate', N'UsageLimit', N'UsedCount', N'IsActive', N'CreatedAt', N'Type', N'OwnerUserId', N'IsWheelPrize', N'Weight') AND [object_id] = OBJECT_ID(N'[Vouchers]'))
        SET IDENTITY_INSERT [Vouchers] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807232949_SeedVouchersAndWheelPrizeTemplates'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260807232949_SeedVouchersAndWheelPrizeTemplates', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808112257_AddPrescriptionPatientIdAndItemInstructions'
)
BEGIN
    ALTER TABLE [Prescriptions] ADD [PatientId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808112257_AddPrescriptionPatientIdAndItemInstructions'
)
BEGIN
    ALTER TABLE [PrescriptionItems] ADD [Instructions] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808112257_AddPrescriptionPatientIdAndItemInstructions'
)
BEGIN
    CREATE INDEX [IX_Prescriptions_PatientId] ON [Prescriptions] ([PatientId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808112257_AddPrescriptionPatientIdAndItemInstructions'
)
BEGIN
    ALTER TABLE [Prescriptions] ADD CONSTRAINT [FK_Prescriptions_AspNetUsers_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808112257_AddPrescriptionPatientIdAndItemInstructions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260808112257_AddPrescriptionPatientIdAndItemInstructions', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808214657_AddNewsArticles'
)
BEGIN
    CREATE TABLE [NewsArticles] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(max) NOT NULL,
        [Excerpt] nvarchar(max) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [Tag] nvarchar(max) NOT NULL,
        [ImageUrl] nvarchar(max) NOT NULL,
        [PublishedDate] datetime2 NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_NewsArticles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808214657_AddNewsArticles'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260808214657_AddNewsArticles', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812131909_AddAuditLog'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NULL,
        [UserName] nvarchar(max) NOT NULL,
        [UserRole] nvarchar(max) NOT NULL,
        [Action] nvarchar(max) NOT NULL,
        [EntityName] nvarchar(450) NOT NULL,
        [EntityId] nvarchar(max) NULL,
        [Description] nvarchar(max) NOT NULL,
        [OldValue] nvarchar(max) NULL,
        [NewValue] nvarchar(max) NULL,
        [IpAddress] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AuditLogs_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812131909_AddAuditLog'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_CreatedAt] ON [AuditLogs] ([CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812131909_AddAuditLog'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_EntityName] ON [AuditLogs] ([EntityName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812131909_AddAuditLog'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_UserId] ON [AuditLogs] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812131909_AddAuditLog'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260812131909_AddAuditLog', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812132406_AddMedicineBarcode'
)
BEGIN
    ALTER TABLE [Medicines] ADD [Barcode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812132406_AddMedicineBarcode'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260812132406_AddMedicineBarcode', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812133323_AddLoyaltyPoints'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [LoyaltyPoints] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812133323_AddLoyaltyPoints'
)
BEGIN
    CREATE TABLE [LoyaltyPointTransactions] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Points] int NOT NULL,
        [Reason] nvarchar(max) NOT NULL,
        [OrderId] int NULL,
        [VoucherId] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_LoyaltyPointTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LoyaltyPointTransactions_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_LoyaltyPointTransactions_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LoyaltyPointTransactions_Vouchers_VoucherId] FOREIGN KEY ([VoucherId]) REFERENCES [Vouchers] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812133323_AddLoyaltyPoints'
)
BEGIN
    CREATE INDEX [IX_LoyaltyPointTransactions_OrderId] ON [LoyaltyPointTransactions] ([OrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812133323_AddLoyaltyPoints'
)
BEGIN
    CREATE INDEX [IX_LoyaltyPointTransactions_UserId] ON [LoyaltyPointTransactions] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812133323_AddLoyaltyPoints'
)
BEGIN
    CREATE INDEX [IX_LoyaltyPointTransactions_VoucherId] ON [LoyaltyPointTransactions] ([VoucherId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812133323_AddLoyaltyPoints'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260812133323_AddLoyaltyPoints', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812142723_FixAuditLogUserFkSetNull'
)
BEGIN
    ALTER TABLE [AuditLogs] DROP CONSTRAINT [FK_AuditLogs_AspNetUsers_UserId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812142723_FixAuditLogUserFkSetNull'
)
BEGIN
    ALTER TABLE [AuditLogs] ADD CONSTRAINT [FK_AuditLogs_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812142723_FixAuditLogUserFkSetNull'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260812142723_FixAuditLogUserFkSetNull', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814010405_AddFlashSaleScheduleAndQuantity'
)
BEGIN
    ALTER TABLE [FlashSales] ADD [EndTime] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814010405_AddFlashSaleScheduleAndQuantity'
)
BEGIN
    ALTER TABLE [FlashSales] ADD [QuantityLimit] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814010405_AddFlashSaleScheduleAndQuantity'
)
BEGIN
    ALTER TABLE [FlashSales] ADD [QuantitySold] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814010405_AddFlashSaleScheduleAndQuantity'
)
BEGIN
    ALTER TABLE [FlashSales] ADD [StartTime] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814010405_AddFlashSaleScheduleAndQuantity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260814010405_AddFlashSaleScheduleAndQuantity', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260817074628_AddPrescriptionItemUnitPrice'
)
BEGIN
    ALTER TABLE [PrescriptionItems] ADD [UnitPrice] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260817074628_AddPrescriptionItemUnitPrice'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260817074628_AddPrescriptionItemUnitPrice', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825034013_AddUserMedicalHistory'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [MedicalHistory] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825034013_AddUserMedicalHistory'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260825034013_AddUserMedicalHistory', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831044850_AddUnitCostPriceSnapshotToInventoryTransaction'
)
BEGIN
    ALTER TABLE [InventoryTransactions] ADD [UnitCostPrice] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831044850_AddUnitCostPriceSnapshotToInventoryTransaction'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260831044850_AddUnitCostPriceSnapshotToInventoryTransaction', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831063614_AddSellPriceToStockBatch'
)
BEGIN
    ALTER TABLE [StockBatches] ADD [SellPrice] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831063614_AddSellPriceToStockBatch'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260831063614_AddSellPriceToStockBatch', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831081639_AddPricedFromBatchIdToMedicine'
)
BEGIN
    ALTER TABLE [Medicines] ADD [PricedFromBatchId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831081639_AddPricedFromBatchIdToMedicine'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260831081639_AddPricedFromBatchIdToMedicine', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831150347_AddPriceAppliedToFlashSale'
)
BEGIN
    ALTER TABLE [FlashSales] ADD [PriceApplied] bit NOT NULL DEFAULT CAST(1 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831150347_AddPriceAppliedToFlashSale'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260831150347_AddPriceAppliedToFlashSale', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901091713_BackfillExportCostPriceSnapshot'
)
BEGIN

    UPDATE b
    SET b.UnitCostPrice = CAST(ISNULL(b.SellPrice, ISNULL(m.Price, 10000)) * 0.65 AS DECIMAL(18,2))
    FROM StockBatches b
    JOIN Medicines m ON m.Id = b.MedicineId
    WHERE b.UnitCostPrice IS NULL;

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901091713_BackfillExportCostPriceSnapshot'
)
BEGIN

    UPDATE t
    SET t.UnitCostPrice = b.UnitCostPrice
    FROM InventoryTransactions t
    JOIN StockBatches b ON b.Id = t.StockBatchId
    WHERE t.Type = 'Export' AND t.UnitCostPrice IS NULL AND b.UnitCostPrice IS NOT NULL;

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901091713_BackfillExportCostPriceSnapshot'
)
BEGIN

    UPDATE t
    SET t.UnitCostPrice = CAST(ISNULL(m.Price, 10000) * 0.65 AS DECIMAL(18,2))
    FROM InventoryTransactions t
    JOIN Medicines m ON m.Id = t.MedicineId
    WHERE t.Type = 'Export' AND t.UnitCostPrice IS NULL;

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901091713_BackfillExportCostPriceSnapshot'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260901091713_BackfillExportCostPriceSnapshot', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901095550_AddPayOsOrderCodeToPayment'
)
BEGIN
    ALTER TABLE [Payments] ADD [PayOsOrderCode] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901095550_AddPayOsOrderCodeToPayment'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260901095550_AddPayOsOrderCodeToPayment', N'8.0.29');
END;
GO

COMMIT;
GO

