# SQL-скрипты SaveData1

Скрипты применяются вручную к БД `SaveData`. Приложение **не запускает** их автоматически.

## Порядок применения на новой/существующей БД

1. **`bd.sql`** — базовый DDL (создание таблиц). Применяется один раз при развёртывании.
2. **`CreateBridgeLogSave.sql`** — таблица `dbo.BridgeLogSave` (если исторически отсутствовала).
3. **`ProductQualityControlAndPermissions.sql`** — добавляет поле `Permissions.Контроль` и lifecycle-колонки в `Product`.
4. **`ProductQualityControlConstraints.sql`** — ранняя версия CHECK/FK. Если ещё не применялся — применить.
5. **`DataHygiene_PreHardening.sql`** — **диагностика** перед hardening-миграцией. Только `SELECT`, ничего не меняет.
6. **`SchemaHardening.sql`** — финальная идемпотентная миграция: сужение типов, `NOT NULL`, уникальные индексы, индексы под запросы, CHECK жизненного цикла и таблица `dbo.ProductPhoto`.

## Процедура безопасного применения

1. **Резервная копия**: `BACKUP DATABASE SaveData TO DISK = 'D:\Backup\SaveData_YYYYMMDD.bak' WITH INIT, CHECKSUM;`.
2. Развернуть backup на тестовом окружении.
3. Запустить `DataHygiene_PreHardening.sql` — все результирующие таблицы должны быть пустыми.
4. Устранить найденные конфликты (дубликаты, NULL, пароли длиннее 256). Шаги 3–4 повторять до «чисто».
5. Запустить `SchemaHardening.sql` — идемпотентный, при повторном запуске только докатит недостающее.
6. Проверить, что приложение работает (логин, просмотр актов, добавление продукта, сохранение работ, добавление фото в MiMi).
7. Повторить 1–6 на проде.

## Откат

Отдельного rollback-скрипта нет — миграция только добавляет ограничения/индексы и **не удаляет** данные.
Если нужно откатить конкретное ограничение/индекс, можно вручную:

```sql
ALTER TABLE dbo.Product       DROP CONSTRAINT CK_Product_QcPassed_Audit;
ALTER TABLE dbo.Product       DROP CONSTRAINT CK_Product_PostWarehouse_User;
ALTER TABLE dbo.Product       DROP CONSTRAINT CK_Product_TestingUnlock_Pair;
ALTER TABLE dbo.Product       DROP CONSTRAINT CK_Product_AssemblyUnlock_Pair;
ALTER TABLE dbo.TechnicalMapAssembly DROP CONSTRAINT CK_TMA_TimeOrder;
ALTER TABLE dbo.TechnicalMapTesting  DROP CONSTRAINT CK_TMT_TimeOrder;

DROP INDEX UX_UsersProfile_UserLogin          ON dbo.UsersProfile;
DROP INDEX UX_Act_ActNumber                   ON dbo.Act;
DROP INDEX UX_Product_ProductSerial           ON dbo.Product;
DROP INDEX UX_Permissions_PermissionsName     ON dbo.Permissions;
DROP INDEX UX_Role_RoleName                   ON dbo.Role;
DROP INDEX UX_Country_CountryName             ON dbo.Country;
DROP INDEX UX_ProducType_TypeName_CountryID   ON dbo.ProducType;
DROP INDEX UX_SavePath_ActNumber              ON dbo.SavePath;

DROP INDEX IX_Product_ActID                              ON dbo.Product;
DROP INDEX IX_Product_PostTestingWarehouseAt             ON dbo.Product;
DROP INDEX IX_TechnicalMapFull_ProductID                 ON dbo.TechnicalMapFull;
DROP INDEX IX_TechnicalMapAssembly_TMID_InProgress       ON dbo.TechnicalMapAssembly;
DROP INDEX IX_TechnicalMapAssembly_TMID_Fault            ON dbo.TechnicalMapAssembly;
DROP INDEX IX_TechnicalMapTesting_TMID_InProgress        ON dbo.TechnicalMapTesting;
DROP INDEX IX_TechnicalMapTesting_TMID_Fault             ON dbo.TechnicalMapTesting;
DROP INDEX IX_BridgeLogSave_ActID_SavedUtc               ON dbo.BridgeLogSave;
DROP INDEX IX_UserWithPermissions_UserID                 ON dbo.UserWithPermissions;

ALTER TABLE dbo.Product DROP CONSTRAINT FK_Product_AssemblyManualUnlockByUser_UsersProfile;

-- Таблица фото (ВНИМАНИЕ: удалит все сохранённые фотографии из БД):
-- DROP TABLE dbo.ProductPhoto;
```

## EDMX

EDMX-модель `SaveData1/Entity/Model1.edmx` **не пересоздаётся** после применения `SchemaHardening.sql`.
Новая таблица `dbo.ProductPhoto` используется из C# через `Database.ExecuteSqlCommand` / `Database.SqlQuery`
в `SaveDataEntitiesPartial.cs` (см. пример `BridgeLogSave`/`SavePath`).

## Контрольные проверки после миграции

```sql
-- все уникальные индексы созданы:
SELECT i.name, OBJECT_NAME(i.object_id) AS TableName
FROM sys.indexes i
WHERE i.is_unique = 1 AND i.name LIKE 'UX_%'
ORDER BY TableName, i.name;

-- все CHECK-ограничения lifecycle активны:
SELECT name, OBJECT_NAME(parent_object_id) AS TableName, is_disabled
FROM sys.check_constraints
WHERE name LIKE 'CK_Product_%' OR name LIKE 'CK_TM_%';

-- таблица фото:
SELECT * FROM sys.tables WHERE name = 'ProductPhoto';
```
