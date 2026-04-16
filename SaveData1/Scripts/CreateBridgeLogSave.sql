-- -----------------------------------------------------------------------------
-- Миграция: dbo.BridgeLogSave (логи ESP32 Bridge).
-- Полный скрипт схемы БД: см. корневой bd.sql (объект BridgeLogSave).
-- Выполните на существующей базе SaveData, если таблица ещё не создана.
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.BridgeLogSave', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BridgeLogSave](
        [BridgeLogSaveID] [int] IDENTITY(1,1) NOT NULL,
        [ActID] [int] NOT NULL,
        [UserID] [int] NOT NULL,
        [SerialNumber] [nvarchar](120) NOT NULL,
        [SavedUtc] [datetime2](7) NOT NULL,
        [UnifiedLogText] [nvarchar](max) NULL,
        [StatusJson] [nvarchar](max) NULL,
        [MavlinkJson] [nvarchar](max) NULL,
     CONSTRAINT [PK_BridgeLogSave] PRIMARY KEY CLUSTERED ([BridgeLogSaveID] ASC)
    );
    ALTER TABLE [dbo].[BridgeLogSave] WITH CHECK ADD CONSTRAINT [FK_BridgeLogSave_Act] FOREIGN KEY([ActID]) REFERENCES [dbo].[Act] ([ActID]);
    ALTER TABLE [dbo].[BridgeLogSave] CHECK CONSTRAINT [FK_BridgeLogSave_Act];
    ALTER TABLE [dbo].[BridgeLogSave] WITH CHECK ADD CONSTRAINT [FK_BridgeLogSave_UsersProfile] FOREIGN KEY([UserID]) REFERENCES [dbo].[UsersProfile] ([UserID]);
    ALTER TABLE [dbo].[BridgeLogSave] CHECK CONSTRAINT [FK_BridgeLogSave_UsersProfile];
END
GO
