-- Дополнительные ограничения целостности для этапов контроля / склада после теста.
-- Выполнить после ProductQualityControlAndPermissions.sql (когда колонки уже есть).

-- Если предыдущий запуск частично создал FK с ON DELETE SET NULL, удаляем и пересоздаём с NO ACTION.
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Product_QualityControlByUser_UsersProfile' AND parent_object_id = OBJECT_ID(N'dbo.Product'))
    ALTER TABLE dbo.Product DROP CONSTRAINT FK_Product_QualityControlByUser_UsersProfile;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Product_PostTestingWarehouseByUser_UsersProfile' AND parent_object_id = OBJECT_ID(N'dbo.Product'))
    ALTER TABLE dbo.Product DROP CONSTRAINT FK_Product_PostTestingWarehouseByUser_UsersProfile;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Product_TestingManualUnlockByUser_UsersProfile' AND parent_object_id = OBJECT_ID(N'dbo.Product'))
    ALTER TABLE dbo.Product DROP CONSTRAINT FK_Product_TestingManualUnlockByUser_UsersProfile;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Product_AssemblyManualUnlockByUser_UsersProfile' AND parent_object_id = OBJECT_ID(N'dbo.Product'))
    ALTER TABLE dbo.Product DROP CONSTRAINT FK_Product_AssemblyManualUnlockByUser_UsersProfile;
GO

-- Некорректные сочетания (если появились до введения ограничений)
IF COL_LENGTH(N'dbo.Product', N'PostTestingWarehouseAt') IS NOT NULL
BEGIN
    UPDATE dbo.Product
    SET PostTestingWarehouseAt = NULL, PostTestingWarehouseByUserID = NULL
    WHERE PostTestingWarehouseAt IS NOT NULL AND QualityControlPassed = 0;
END
GO

-- Склад после теста только при пройденном контроле
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Product_PostWarehouseRequiresQc' AND parent_object_id = OBJECT_ID(N'dbo.Product'))
BEGIN
    ALTER TABLE dbo.Product WITH NOCHECK
    ADD CONSTRAINT CK_Product_PostWarehouseRequiresQc
    CHECK (PostTestingWarehouseAt IS NULL OR QualityControlPassed = 1);
    ALTER TABLE dbo.Product CHECK CONSTRAINT CK_Product_PostWarehouseRequiresQc;
END
GO

-- Ссылочная целостность на UsersProfile.
-- Важно: для одной таблицы Product нельзя задать несколько FK на UsersProfile с ON DELETE SET NULL —
-- SQL Server выдаёт ошибку 1785 (multiple cascade paths). Поэтому здесь NO ACTION: перед удалением
-- пользователя из БД нужно вручную обнулить ссылающиеся поля в Product (или переназначить).

IF COL_LENGTH(N'dbo.Product', N'QualityControlByUserID') IS NOT NULL
BEGIN
    UPDATE p SET QualityControlByUserID = NULL
    FROM dbo.Product p
    WHERE p.QualityControlByUserID IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.UsersProfile u WHERE u.UserID = p.QualityControlByUserID);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Product_QualityControlByUser_UsersProfile')
        ALTER TABLE dbo.Product ADD CONSTRAINT FK_Product_QualityControlByUser_UsersProfile
            FOREIGN KEY (QualityControlByUserID) REFERENCES dbo.UsersProfile(UserID)
            ON DELETE NO ACTION ON UPDATE NO ACTION;

    UPDATE p SET PostTestingWarehouseByUserID = NULL
    FROM dbo.Product p
    WHERE p.PostTestingWarehouseByUserID IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.UsersProfile u WHERE u.UserID = p.PostTestingWarehouseByUserID);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Product_PostTestingWarehouseByUser_UsersProfile')
        ALTER TABLE dbo.Product ADD CONSTRAINT FK_Product_PostTestingWarehouseByUser_UsersProfile
            FOREIGN KEY (PostTestingWarehouseByUserID) REFERENCES dbo.UsersProfile(UserID)
            ON DELETE NO ACTION ON UPDATE NO ACTION;

    UPDATE p SET TestingManualUnlockByUserID = NULL
    FROM dbo.Product p
    WHERE p.TestingManualUnlockByUserID IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.UsersProfile u WHERE u.UserID = p.TestingManualUnlockByUserID);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Product_TestingManualUnlockByUser_UsersProfile')
        ALTER TABLE dbo.Product ADD CONSTRAINT FK_Product_TestingManualUnlockByUser_UsersProfile
            FOREIGN KEY (TestingManualUnlockByUserID) REFERENCES dbo.UsersProfile(UserID)
            ON DELETE NO ACTION ON UPDATE NO ACTION;
END
GO

IF COL_LENGTH(N'dbo.Product', N'AssemblyManualUnlockByUserID') IS NOT NULL
BEGIN
    UPDATE p SET AssemblyManualUnlockByUserID = NULL
    FROM dbo.Product p
    WHERE p.AssemblyManualUnlockByUserID IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.UsersProfile u WHERE u.UserID = p.AssemblyManualUnlockByUserID);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Product_AssemblyManualUnlockByUser_UsersProfile')
        ALTER TABLE dbo.Product ADD CONSTRAINT FK_Product_AssemblyManualUnlockByUser_UsersProfile
            FOREIGN KEY (AssemblyManualUnlockByUserID) REFERENCES dbo.UsersProfile(UserID)
            ON DELETE NO ACTION ON UPDATE NO ACTION;
END
GO
