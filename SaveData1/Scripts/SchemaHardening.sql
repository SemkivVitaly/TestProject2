/* ============================================================================
   SchemaHardening.sql
   Идемпотентный миграционный скрипт: сужение типов, NOT NULL, уникальные и
   недостающие индексы, FK, CHECK-ограничения жизненного цикла, таблица
   dbo.ProductPhoto для функции загрузки фотографий продукта.

   ВАЖНО:
     1) Предварительно прогнать SaveData1/Scripts/DataHygiene_PreHardening.sql
        и устранить все найденные конфликты.
     2) Сделать резервную копию БД (full backup).
     3) Запускать на тестовой БД, потом на проде.

   Скрипт безопасно перезапускать — каждая секция проверяет текущее состояние.
============================================================================ */
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

USE [SaveData];
GO

PRINT N'=== SchemaHardening.sql старт ===';

/* ---------------------------------------------------------------- */
/* 1. Типы колонок                                                 */
/* ---------------------------------------------------------------- */

-- UsersProfile.UserPassword: nvarchar(max) -> nvarchar(256)
IF EXISTS (
    SELECT 1
    FROM sys.columns c
    JOIN sys.objects o ON o.object_id = c.object_id
    WHERE o.name = N'UsersProfile' AND c.name = N'UserPassword'
      AND c.max_length = -1 -- nvarchar(max)
)
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.UsersProfile WHERE LEN(UserPassword) > 256)
    BEGIN
        RAISERROR(N'UsersProfile.UserPassword: найдены значения длиннее 256 символов. Сначала укоротите пароли/перехешируйте.', 16, 1);
    END
    ELSE
    BEGIN
        PRINT N'[1] UsersProfile.UserPassword: nvarchar(max) -> nvarchar(256)';
        ALTER TABLE dbo.UsersProfile ALTER COLUMN UserPassword nvarchar(256) NOT NULL;
    END
END

-- SavePath.ActNumber: nvarchar(max) NULL -> nvarchar(128) NOT NULL
IF EXISTS (
    SELECT 1
    FROM sys.columns c
    JOIN sys.objects o ON o.object_id = c.object_id
    WHERE o.name = N'SavePath' AND c.name = N'ActNumber'
      AND c.max_length = -1
)
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.SavePath WHERE ActNumber IS NULL OR LTRIM(RTRIM(ActNumber)) = N'')
    BEGIN
        RAISERROR(N'SavePath.ActNumber: есть NULL/пустые значения. Исправьте их в диагностическом скрипте.', 16, 1);
    END
    ELSE
    BEGIN
        PRINT N'[1] SavePath.ActNumber: nvarchar(max) -> nvarchar(128) NOT NULL';
        ALTER TABLE dbo.SavePath ALTER COLUMN ActNumber nvarchar(128) NOT NULL;
    END
END

/* ---------------------------------------------------------------- */
/* 2. NOT NULL (Role.RoleName)                                     */
/* ---------------------------------------------------------------- */
IF EXISTS (
    SELECT 1
    FROM sys.columns c
    JOIN sys.objects o ON o.object_id = c.object_id
    WHERE o.name = N'Role' AND c.name = N'RoleName'
      AND c.is_nullable = 1
)
BEGIN
    PRINT N'[2] Role.RoleName: backfill NULL и ALTER NOT NULL';
    UPDATE dbo.Role SET RoleName = N'Unknown' WHERE RoleName IS NULL;
    ALTER TABLE dbo.Role ALTER COLUMN RoleName nvarchar(25) NOT NULL;
END

/* ---------------------------------------------------------------- */
/* 3. Уникальные индексы                                           */
/* ---------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_UsersProfile_UserLogin' AND object_id = OBJECT_ID(N'dbo.UsersProfile'))
BEGIN
    PRINT N'[3] CREATE UNIQUE UX_UsersProfile_UserLogin';
    CREATE UNIQUE NONCLUSTERED INDEX UX_UsersProfile_UserLogin ON dbo.UsersProfile(UserLogin);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Act_ActNumber' AND object_id = OBJECT_ID(N'dbo.Act'))
BEGIN
    PRINT N'[3] CREATE UNIQUE UX_Act_ActNumber';
    CREATE UNIQUE NONCLUSTERED INDEX UX_Act_ActNumber ON dbo.Act(ActNumber);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Product_ProductSerial' AND object_id = OBJECT_ID(N'dbo.Product'))
BEGIN
    PRINT N'[3] CREATE UNIQUE UX_Product_ProductSerial';
    CREATE UNIQUE NONCLUSTERED INDEX UX_Product_ProductSerial ON dbo.Product(ProductSerial);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Permissions_PermissionsName' AND object_id = OBJECT_ID(N'dbo.Permissions'))
BEGIN
    PRINT N'[3] CREATE UNIQUE UX_Permissions_PermissionsName';
    CREATE UNIQUE NONCLUSTERED INDEX UX_Permissions_PermissionsName ON dbo.Permissions(PermissionsName);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Role_RoleName' AND object_id = OBJECT_ID(N'dbo.Role'))
BEGIN
    PRINT N'[3] CREATE UNIQUE UX_Role_RoleName';
    CREATE UNIQUE NONCLUSTERED INDEX UX_Role_RoleName ON dbo.Role(RoleName);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Country_CountryName' AND object_id = OBJECT_ID(N'dbo.Country'))
BEGIN
    PRINT N'[3] CREATE UNIQUE UX_Country_CountryName';
    CREATE UNIQUE NONCLUSTERED INDEX UX_Country_CountryName ON dbo.Country(CountryName);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ProducType_TypeName_CountryID' AND object_id = OBJECT_ID(N'dbo.ProducType'))
BEGIN
    PRINT N'[3] CREATE UNIQUE UX_ProducType_TypeName_CountryID';
    CREATE UNIQUE NONCLUSTERED INDEX UX_ProducType_TypeName_CountryID ON dbo.ProducType(TypeName, CountryID);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_SavePath_ActNumber' AND object_id = OBJECT_ID(N'dbo.SavePath'))
BEGIN
    PRINT N'[3] CREATE UNIQUE UX_SavePath_ActNumber';
    CREATE UNIQUE NONCLUSTERED INDEX UX_SavePath_ActNumber ON dbo.SavePath(ActNumber);
END

/* ---------------------------------------------------------------- */
/* 4. Обычные индексы под типичные запросы                         */
/* ---------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Product_ActID' AND object_id = OBJECT_ID(N'dbo.Product'))
BEGIN
    PRINT N'[4] CREATE IX_Product_ActID (INCLUDE ProductSerial, TypeID, PostTestingWarehouseAt, QualityControlPassed)';
    CREATE NONCLUSTERED INDEX IX_Product_ActID
      ON dbo.Product(ActID)
      INCLUDE (ProductSerial, TypeID, PostTestingWarehouseAt, QualityControlPassed);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Product_PostTestingWarehouseAt' AND object_id = OBJECT_ID(N'dbo.Product'))
BEGIN
    PRINT N'[4] CREATE IX_Product_PostTestingWarehouseAt (filtered)';
    CREATE NONCLUSTERED INDEX IX_Product_PostTestingWarehouseAt
      ON dbo.Product(PostTestingWarehouseAt)
      WHERE PostTestingWarehouseAt IS NOT NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TechnicalMapFull_ProductID' AND object_id = OBJECT_ID(N'dbo.TechnicalMapFull'))
BEGIN
    PRINT N'[4] CREATE IX_TechnicalMapFull_ProductID';
    CREATE NONCLUSTERED INDEX IX_TechnicalMapFull_ProductID
      ON dbo.TechnicalMapFull(ProductID)
      INCLUDE (TMID, Inspection);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TechnicalMapAssembly_TMID_InProgress' AND object_id = OBJECT_ID(N'dbo.TechnicalMapAssembly'))
BEGIN
    PRINT N'[4] CREATE IX_TechnicalMapAssembly_TMID_InProgress';
    CREATE NONCLUSTERED INDEX IX_TechnicalMapAssembly_TMID_InProgress
      ON dbo.TechnicalMapAssembly(TMID, InProgress);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TechnicalMapAssembly_TMID_Fault' AND object_id = OBJECT_ID(N'dbo.TechnicalMapAssembly'))
BEGIN
    PRINT N'[4] CREATE IX_TechnicalMapAssembly_TMID_Fault';
    CREATE NONCLUSTERED INDEX IX_TechnicalMapAssembly_TMID_Fault
      ON dbo.TechnicalMapAssembly(TMID, Fault);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TechnicalMapTesting_TMID_InProgress' AND object_id = OBJECT_ID(N'dbo.TechnicalMapTesting'))
BEGIN
    PRINT N'[4] CREATE IX_TechnicalMapTesting_TMID_InProgress';
    CREATE NONCLUSTERED INDEX IX_TechnicalMapTesting_TMID_InProgress
      ON dbo.TechnicalMapTesting(TMID, InProgress);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TechnicalMapTesting_TMID_Fault' AND object_id = OBJECT_ID(N'dbo.TechnicalMapTesting'))
BEGIN
    PRINT N'[4] CREATE IX_TechnicalMapTesting_TMID_Fault';
    CREATE NONCLUSTERED INDEX IX_TechnicalMapTesting_TMID_Fault
      ON dbo.TechnicalMapTesting(TMID, Fault);
END

IF OBJECT_ID(N'dbo.BridgeLogSave', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BridgeLogSave_ActID_SavedUtc' AND object_id = OBJECT_ID(N'dbo.BridgeLogSave'))
BEGIN
    PRINT N'[4] CREATE IX_BridgeLogSave_ActID_SavedUtc';
    CREATE NONCLUSTERED INDEX IX_BridgeLogSave_ActID_SavedUtc
      ON dbo.BridgeLogSave(ActID, SavedUtc DESC);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UserWithPermissions_UserID' AND object_id = OBJECT_ID(N'dbo.UserWithPermissions'))
BEGIN
    PRINT N'[4] CREATE IX_UserWithPermissions_UserID';
    CREATE NONCLUSTERED INDEX IX_UserWithPermissions_UserID
      ON dbo.UserWithPermissions(UserID)
      INCLUDE (PermissionsID);
END

/* ---------------------------------------------------------------- */
/* 5. Недостающий FK: Product.AssemblyManualUnlockByUserID         */
/* ---------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Product_AssemblyManualUnlockByUser_UsersProfile')
BEGIN
    PRINT N'[5] ADD FK_Product_AssemblyManualUnlockByUser_UsersProfile';
    ALTER TABLE dbo.Product
      ADD CONSTRAINT FK_Product_AssemblyManualUnlockByUser_UsersProfile
        FOREIGN KEY (AssemblyManualUnlockByUserID) REFERENCES dbo.UsersProfile(UserID);
END

/* ---------------------------------------------------------------- */
/* 6. CHECK-ограничения жизненного цикла Product                   */
/* ---------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Product_QcPassed_Audit')
BEGIN
    PRINT N'[6] ADD CK_Product_QcPassed_Audit';
    ALTER TABLE dbo.Product
      ADD CONSTRAINT CK_Product_QcPassed_Audit
        CHECK (QualityControlPassed = 0
            OR (QualityControlPassedUtc IS NOT NULL AND QualityControlByUserID IS NOT NULL));
END

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Product_PostWarehouse_User')
BEGIN
    PRINT N'[6] ADD CK_Product_PostWarehouse_User';
    ALTER TABLE dbo.Product
      ADD CONSTRAINT CK_Product_PostWarehouse_User
        CHECK (PostTestingWarehouseAt IS NULL OR PostTestingWarehouseByUserID IS NOT NULL);
END

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Product_TestingUnlock_Pair')
BEGIN
    PRINT N'[6] ADD CK_Product_TestingUnlock_Pair';
    ALTER TABLE dbo.Product
      ADD CONSTRAINT CK_Product_TestingUnlock_Pair
        CHECK ((TestingManualUnlockByUserID IS NULL AND TestingManualUnlockUtc IS NULL)
            OR (TestingManualUnlockByUserID IS NOT NULL AND TestingManualUnlockUtc IS NOT NULL));
END

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Product_AssemblyUnlock_Pair')
BEGIN
    PRINT N'[6] ADD CK_Product_AssemblyUnlock_Pair';
    ALTER TABLE dbo.Product
      ADD CONSTRAINT CK_Product_AssemblyUnlock_Pair
        CHECK ((AssemblyManualUnlockByUserID IS NULL AND AssemblyManualUnlockUtc IS NULL)
            OR (AssemblyManualUnlockByUserID IS NOT NULL AND AssemblyManualUnlockUtc IS NOT NULL));
END

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_TMA_TimeOrder')
BEGIN
    PRINT N'[6] ADD CK_TMA_TimeOrder';
    ALTER TABLE dbo.TechnicalMapAssembly
      ADD CONSTRAINT CK_TMA_TimeOrder CHECK (TimeEnd >= TimeStart);
END

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_TMT_TimeOrder')
BEGIN
    PRINT N'[6] ADD CK_TMT_TimeOrder';
    ALTER TABLE dbo.TechnicalMapTesting
      ADD CONSTRAINT CK_TMT_TimeOrder CHECK (TimeEnd >= TimeStart);
END

/* ---------------------------------------------------------------- */
/* 7. Таблица ProductPhoto (для Phase 6: фото на ProductWorkForm)  */
/* ---------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.ProductPhoto', N'U') IS NULL
BEGIN
    PRINT N'[7] CREATE TABLE dbo.ProductPhoto + UX/IX';
    CREATE TABLE dbo.ProductPhoto (
        ProductPhotoID     int            IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_ProductPhoto PRIMARY KEY CLUSTERED,
        ProductID          int            NOT NULL,
        SequenceNo         int            NOT NULL,            -- (1), (2), (3)…
        Stage              tinyint        NOT NULL,            -- 1=Assembly, 2=Testing
        TMAID              int            NULL,
        TMTID              int            NULL,
        FileName           nvarchar(260)  NOT NULL,
        RelativePath       nvarchar(500)  NOT NULL,
        ContentType        nvarchar(100)  NOT NULL,
        ByteLength         int            NOT NULL,
        PhotoBytes         varbinary(MAX) NOT NULL,
        SavedUtc           datetime2(7)   NOT NULL
            CONSTRAINT DF_ProductPhoto_SavedUtc DEFAULT SYSUTCDATETIME(),
        ByUserID           int            NOT NULL,
        CONSTRAINT FK_ProductPhoto_Product
            FOREIGN KEY (ProductID) REFERENCES dbo.Product(ProductID),
        CONSTRAINT FK_ProductPhoto_User
            FOREIGN KEY (ByUserID)  REFERENCES dbo.UsersProfile(UserID),
        CONSTRAINT FK_ProductPhoto_TMA
            FOREIGN KEY (TMAID)     REFERENCES dbo.TechnicalMapAssembly(TMAID),
        CONSTRAINT FK_ProductPhoto_TMT
            FOREIGN KEY (TMTID)     REFERENCES dbo.TechnicalMapTesting(TMTID),
        CONSTRAINT CK_ProductPhoto_Stage CHECK (Stage IN (1,2)),
        CONSTRAINT CK_ProductPhoto_StageLink CHECK (
            (Stage = 1 AND TMAID IS NOT NULL AND TMTID IS NULL) OR
            (Stage = 2 AND TMTID IS NOT NULL AND TMAID IS NULL))
    );

    CREATE UNIQUE NONCLUSTERED INDEX UX_ProductPhoto_Product_Seq
      ON dbo.ProductPhoto(ProductID, SequenceNo);

    CREATE NONCLUSTERED INDEX IX_ProductPhoto_Product_Saved
      ON dbo.ProductPhoto(ProductID, SavedUtc DESC)
      INCLUDE (FileName, Stage, ByUserID);
END

PRINT N'=== SchemaHardening.sql завершён ===';
GO
