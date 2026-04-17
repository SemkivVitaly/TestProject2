/* ============================================================================
   DataHygiene_PreHardening.sql
   Назначение: ДИАГНОСТИЧЕСКИЙ скрипт. Выводит потенциальные проблемы
   (дубликаты, NULL в обязательных колонках, отсутствующие связи) ДО
   применения SchemaHardening.sql.

   Как использовать:
     1) Запустить на тестовой копии БД.
     2) Просмотреть каждый result set:
          * Если пуст — проблем нет.
          * Если есть строки — исправить данные вручную, затем повторить.
     3) Когда все result set'ы пустые, можно применять SchemaHardening.sql.

   Скрипт НИЧЕГО не меняет в данных/схеме (только SELECT).
============================================================================ */
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

USE [SaveData];
GO

PRINT N'=== DataHygiene_PreHardening.sql — проверка данных перед миграцией ===';

/* ---------- UsersProfile: логины должны быть уникальны ---------- */
PRINT N'[1/10] Дубликаты UsersProfile.UserLogin (должен быть пустой):';
SELECT UserLogin, COUNT(*) AS Cnt
FROM dbo.UsersProfile
GROUP BY UserLogin
HAVING COUNT(*) > 1
ORDER BY UserLogin;

/* ---------- UsersProfile: длина пароля для безопасного сжатия до nvarchar(256) ---------- */
PRINT N'[2/10] Пароли длиннее 256 символов (должно быть 0):';
SELECT UserID, UserLogin, LEN(UserPassword) AS PasswordLength
FROM dbo.UsersProfile
WHERE LEN(UserPassword) > 256;

/* ---------- Role.RoleName: NOT NULL перед апгрейдом ---------- */
PRINT N'[3/10] Роли с NULL RoleName (будут заменены на ''Unknown''):';
SELECT RoleID
FROM dbo.Role
WHERE RoleName IS NULL;

PRINT N'[4/10] Дубликаты Role.RoleName (должен быть пустой):';
SELECT RoleName, COUNT(*) AS Cnt
FROM dbo.Role
WHERE RoleName IS NOT NULL
GROUP BY RoleName
HAVING COUNT(*) > 1;

/* ---------- Act: уникальность номера акта ---------- */
PRINT N'[5/10] Дубликаты Act.ActNumber (должен быть пустой):';
SELECT ActNumber, COUNT(*) AS Cnt
FROM dbo.Act
GROUP BY ActNumber
HAVING COUNT(*) > 1;

/* ---------- Product: уникальность серийника ---------- */
PRINT N'[6/10] Дубликаты Product.ProductSerial (должен быть пустой):';
SELECT ProductSerial, COUNT(*) AS Cnt
FROM dbo.Product
GROUP BY ProductSerial
HAVING COUNT(*) > 1;

/* ---------- Permissions / Country / ProducType ---------- */
PRINT N'[7/10] Дубликаты Permissions.PermissionsName:';
SELECT PermissionsName, COUNT(*) AS Cnt
FROM dbo.Permissions
GROUP BY PermissionsName
HAVING COUNT(*) > 1;

PRINT N'[8/10] Дубликаты Country.CountryName:';
SELECT CountryName, COUNT(*) AS Cnt
FROM dbo.Country
GROUP BY CountryName
HAVING COUNT(*) > 1;

PRINT N'[9/10] Дубликаты ProducType (TypeName, CountryID):';
SELECT TypeName, CountryID, COUNT(*) AS Cnt
FROM dbo.ProducType
GROUP BY TypeName, CountryID
HAVING COUNT(*) > 1;

/* ---------- SavePath: ActNumber должен стать NOT NULL и уникальным ---------- */
PRINT N'[10/10] SavePath: строки с NULL/пустым ActNumber и дубликаты:';
SELECT PathID, SavePath, ActNumber
FROM dbo.SavePath
WHERE ActNumber IS NULL OR LTRIM(RTRIM(ActNumber)) = N'';

SELECT ActNumber, COUNT(*) AS Cnt
FROM dbo.SavePath
WHERE ActNumber IS NOT NULL AND LTRIM(RTRIM(ActNumber)) <> N''
GROUP BY ActNumber
HAVING COUNT(*) > 1;

/* ---------- Product lifecycle: проверяем согласованность флагов ---------- */
PRINT N'[+] Product.QualityControlPassed=1 без даты/пользователя (CK_Product_QcPassed_Audit ждёт согласованности):';
SELECT ProductID, ProductSerial, QualityControlPassed, QualityControlPassedUtc, QualityControlByUserID
FROM dbo.Product
WHERE QualityControlPassed = 1
  AND (QualityControlPassedUtc IS NULL OR QualityControlByUserID IS NULL);

PRINT N'[+] Product.PostTestingWarehouseAt без пользователя (CK_Product_PostWarehouse_User):';
SELECT ProductID, ProductSerial, PostTestingWarehouseAt, PostTestingWarehouseByUserID
FROM dbo.Product
WHERE PostTestingWarehouseAt IS NOT NULL AND PostTestingWarehouseByUserID IS NULL;

PRINT N'[+] Product: непарные TestingManualUnlock (CK_Product_TestingUnlock_Pair):';
SELECT ProductID, ProductSerial, TestingManualUnlockByUserID, TestingManualUnlockUtc
FROM dbo.Product
WHERE (TestingManualUnlockByUserID IS NULL) <> (TestingManualUnlockUtc IS NULL);

PRINT N'[+] Product: непарные AssemblyManualUnlock (CK_Product_AssemblyUnlock_Pair):';
SELECT ProductID, ProductSerial, AssemblyManualUnlockByUserID, AssemblyManualUnlockUtc
FROM dbo.Product
WHERE (AssemblyManualUnlockByUserID IS NULL) <> (AssemblyManualUnlockUtc IS NULL);

/* ---------- TechnicalMap*: TimeEnd >= TimeStart ---------- */
PRINT N'[+] TechnicalMapAssembly: TimeEnd < TimeStart (CK_TMA_TimeOrder):';
SELECT TMAID, [Date], TimeStart, TimeEnd
FROM dbo.TechnicalMapAssembly
WHERE TimeEnd < TimeStart;

PRINT N'[+] TechnicalMapTesting: TimeEnd < TimeStart (CK_TMT_TimeOrder):';
SELECT TMTID, [Date], TimeStart, TimeEnd
FROM dbo.TechnicalMapTesting
WHERE TimeEnd < TimeStart;

PRINT N'=== Проверка завершена. Устраните найденные записи ДО запуска SchemaHardening.sql. ===';
GO
