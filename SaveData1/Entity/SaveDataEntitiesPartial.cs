using System;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core.EntityClient;
using System.Data.SqlClient;
using System.Linq;

namespace SaveData1.Entity
{
    public partial class SaveDataEntities2
    {
        public SaveDataEntities2(string entityConnectionString)
            : base(new EntityConnection(entityConnectionString), contextOwnsConnection: true)
        {
        }

        /// <summary>Возвращает базовый путь папки акта из SavePath.</summary>
        public string GetSavePathForAct(string actNumber)
        {
            var result = this.Database.SqlQuery<string>(
                "SELECT TOP 1 SavePath FROM dbo.SavePath WHERE ActNumber = @p0",
                actNumber).FirstOrDefault<string>();
            return result;
        }

        /// <summary>Сохранение базового пути папки акта в SavePath.</summary>
        public void SetSavePathForAct(string actNumber, string basePath)
        {
            var existing = this.Database.SqlQuery<int>(
                "SELECT COUNT(*) FROM dbo.SavePath WHERE ActNumber = @p0",
                actNumber).FirstOrDefault<int>();

            if (existing > 0)
            {
                this.Database.ExecuteSqlCommand(
                    "UPDATE dbo.SavePath SET SavePath = @p0 WHERE ActNumber = @p1",
                    basePath, actNumber);
            }
            else
            {
                this.Database.ExecuteSqlCommand(
                    "INSERT INTO dbo.SavePath (SavePath, ActNumber) VALUES (@p0, @p1)",
                    basePath, actNumber);
            }
        }

        /// <summary>Запись факта сохранения лога Bridge (см. <c>Scripts/CreateBridgeLogSave.sql</c>).</summary>
        public void InsertBridgeLogSave(int actId, int userId, string serialNumber,
            string unifiedLogText, string statusJson, string mavlinkJson)
        {
            if (string.IsNullOrWhiteSpace(serialNumber))
                throw new ArgumentException("Серийный номер не задан.", nameof(serialNumber));

            const string sql = @"INSERT INTO dbo.BridgeLogSave (ActID, UserID, SerialNumber, SavedUtc, UnifiedLogText, StatusJson, MavlinkJson)
VALUES (@a, @u, @s, SYSUTCDATETIME(), @t, @j, @m)";

            Database.ExecuteSqlCommand(sql,
                new SqlParameter("@a", actId),
                new SqlParameter("@u", userId),
                new SqlParameter("@s", serialNumber.Trim()),
                new SqlParameter("@t", SqlDbType.NVarChar, -1) { Value = (object)unifiedLogText ?? DBNull.Value },
                new SqlParameter("@j", SqlDbType.NVarChar, -1) { Value = (object)statusJson ?? DBNull.Value },
                new SqlParameter("@m", SqlDbType.NVarChar, -1) { Value = (object)mavlinkJson ?? DBNull.Value });
        }
    }
}
