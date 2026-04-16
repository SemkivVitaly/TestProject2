-- Разрешение «Контроль» и поля жизненного цикла продукта (контроль / склад после теста / разблокировка теста без QR).
-- Выполнить вручную на целевой БД перед запуском обновлённого приложения.

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE PermissionsName = N'Контроль')
    INSERT INTO dbo.Permissions (PermissionsName) VALUES (N'Контроль');
GO

IF COL_LENGTH('dbo.Product', 'QualityControlPassed') IS NULL
BEGIN
    ALTER TABLE dbo.Product ADD
        QualityControlPassed bit NOT NULL CONSTRAINT DF_Product_QualityControlPassed DEFAULT 0,
        QualityControlPassedUtc datetime2(7) NULL,
        QualityControlByUserID int NULL,
        PostTestingWarehouseAt datetime2(7) NULL,
        PostTestingWarehouseByUserID int NULL,
        TestingManualUnlockByUserID int NULL,
        TestingManualUnlockUtc datetime2(7) NULL;
END
GO

-- Разблокировка сборки без QR (двойной клик после разрешения администратора).
IF COL_LENGTH(N'dbo.Product', N'AssemblyManualUnlockByUserID') IS NULL
BEGIN
    ALTER TABLE dbo.Product ADD
        AssemblyManualUnlockByUserID int NULL,
        AssemblyManualUnlockUtc datetime2(7) NULL;
END
GO

-- Далее (рекомендуется): выполнить ProductQualityControlConstraints.sql — CHECK и FK на UsersProfile.
