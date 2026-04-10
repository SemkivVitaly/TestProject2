using System;
using System.Collections.Generic;
using System.Linq;
using SaveData1.Entity;

namespace SaveData1.Helpers
{
    /// <summary>Связь записей с описаниями брака (Error, сборка, тестирование).</summary>
    public static class FaultDescriptionHelper
    {
        /// <summary>Присвоение списка описаний брака записи Error.</summary>
        public static void SetErrorDescriptions(int errorId, IEnumerable<int> descriptionIds)
        {
            var ids = descriptionIds?.Distinct().ToList() ?? new List<int>();
            using (var ctx = ConnectionHelper.CreateContext())
            {
                try
                {
                    ctx.Database.ExecuteSqlCommand("DELETE FROM dbo.ErrorDescription WHERE ErrorID = @p0", errorId);
                    foreach (int descId in ids)
                        ctx.Database.ExecuteSqlCommand("INSERT INTO dbo.ErrorDescription (ErrorID, DescriptionID) VALUES (@p0, @p1)", errorId, descId);
                }
                catch { /* таблица может отсутствовать до выполнения скрипта */ }
            }
        }

        /// <summary>Присвоение списка описаний брака записи сборки.</summary>
        public static void SetAssemblyFaultDescriptions(int tmaId, IEnumerable<int> descriptionIds)
        {
            var ids = descriptionIds?.Distinct().ToList() ?? new List<int>();
            using (var ctx = ConnectionHelper.CreateContext())
            {
                try
                {
                    ctx.Database.ExecuteSqlCommand("DELETE FROM dbo.AssemblyFaultDescription WHERE TMAID = @p0", tmaId);
                    foreach (int descId in ids)
                        ctx.Database.ExecuteSqlCommand("INSERT INTO dbo.AssemblyFaultDescription (TMAID, DescriptionID) VALUES (@p0, @p1)", tmaId, descId);
                }
                catch { /* таблица может отсутствовать */ }
            }
        }

        /// <summary>Присвоение списка описаний брака записи тестирования.</summary>
        public static void SetTestingFaultDescriptions(int tmtId, IEnumerable<int> descriptionIds)
        {
            var ids = descriptionIds?.Distinct().ToList() ?? new List<int>();
            using (var ctx = ConnectionHelper.CreateContext())
            {
                try
                {
                    ctx.Database.ExecuteSqlCommand("DELETE FROM dbo.TestingFaultDescription WHERE TMTID = @p0", tmtId);
                    foreach (int descId in ids)
                        ctx.Database.ExecuteSqlCommand("INSERT INTO dbo.TestingFaultDescription (TMTID, DescriptionID) VALUES (@p0, @p1)", tmtId, descId);
                }
                catch { }
            }
        }

        /// <summary>Тексты описаний брака для записи Error.</summary>
        public static List<string> GetErrorDefectTexts(int errorId, int? tmId)
        {
            using (var ctx = ConnectionHelper.CreateContext())
            {
                try
                {
                    var fromError = ctx.Database.SqlQuery<int>(
                        "SELECT DescriptionID FROM dbo.ErrorDescription WHERE ErrorID = @p0", errorId).ToList();
                    if (fromError.Any())
                    {
                        var descs = ctx.Description.Where(d => fromError.Contains(d.DescriptionID)).Select(d => d.DescriptionText).ToList();
                        return descs.Where(x => !string.IsNullOrEmpty(x)).ToList();
                    }
                    if (tmId.HasValue)
                        return GetDefectTextsByTmId(ctx, tmId.Value);
                }
                catch { }
                return new List<string>();
            }
        }

        /// <summary>Тексты описаний брака по идентификатору техкарты.</summary>
        public static List<string> GetDefectTextsByTmId(System.Data.Entity.DbContext ctx, int tmId)
        {
            var result = new List<string>();
            try
            {
                var asmId = ctx.Database.SqlQuery<int>("SELECT TMAID FROM dbo.TechnicalMapAssembly WHERE TMID = @p0 AND Fault = 1", tmId).FirstOrDefault();
                if (asmId != 0)
                {
                    var asmDescIds = ctx.Database.SqlQuery<int>("SELECT DescriptionID FROM dbo.AssemblyFaultDescription WHERE TMAID = @p0", asmId).ToList();
                    if (!asmDescIds.Any())
                    {
                        var single = ctx.Database.SqlQuery<int?>("SELECT DescriptionID FROM dbo.TechnicalMapAssembly WHERE TMAID = @p0", asmId).FirstOrDefault();
                        if (single.HasValue && single.Value != 0) asmDescIds.Add(single.Value);
                    }
                    foreach (int id in asmDescIds)
                    {
                        var t = ctx.Database.SqlQuery<string>("SELECT DescriptionText FROM dbo.Description WHERE DescriptionID = @p0", id).FirstOrDefault();
                        if (!string.IsNullOrEmpty(t)) result.Add(t);
                    }
                }
                var tmtId = ctx.Database.SqlQuery<int>("SELECT TMTID FROM dbo.TechnicalMapTesting WHERE TMID = @p0 AND Fault = 1", tmId).FirstOrDefault();
                if (tmtId != 0)
                {
                    var tstDescIds = ctx.Database.SqlQuery<int>("SELECT DescriptionID FROM dbo.TestingFaultDescription WHERE TMTID = @p0", tmtId).ToList();
                    if (!tstDescIds.Any())
                    {
                        var single = ctx.Database.SqlQuery<int?>("SELECT DescriptionID FROM dbo.TechnicalMapTesting WHERE TMTID = @p0", tmtId).FirstOrDefault();
                        if (single.HasValue && single.Value != 0) tstDescIds.Add(single.Value);
                    }
                    foreach (int id in tstDescIds)
                    {
                        var t = ctx.Database.SqlQuery<string>("SELECT DescriptionText FROM dbo.Description WHERE DescriptionID = @p0", id).FirstOrDefault();
                        if (!string.IsNullOrEmpty(t) && !result.Contains(t)) result.Add(t);
                    }
                }
            }
            catch { }
            return result;
        }

        /// <summary>Тексты описаний брака для записи сборки.</summary>
        public static string GetAssemblyFaultTexts(int tmaId)
        {
            using (var ctx = ConnectionHelper.CreateContext())
            {
                try
                {
                    var ids = ctx.Database.SqlQuery<int>("SELECT DescriptionID FROM dbo.AssemblyFaultDescription WHERE TMAID = @p0", tmaId).ToList();
                    if (!ids.Any())
                    {
                        var one = ctx.Database.SqlQuery<int?>("SELECT DescriptionID FROM dbo.TechnicalMapAssembly WHERE TMAID = @p0", tmaId).FirstOrDefault();
                        if (one.HasValue && one.Value != 0) ids.Add(one.Value);
                    }
                    var texts = ctx.Description.Where(d => ids.Contains(d.DescriptionID)).Select(d => d.DescriptionText).Where(x => !string.IsNullOrEmpty(x)).ToList();
                    return string.Join(", ", texts);
                }
                catch { }
            }
            return "";
        }

        /// <summary>Тексты описаний брака для записи тестирования.</summary>
        public static string GetTestingFaultTexts(int tmtId)
        {
            using (var ctx = ConnectionHelper.CreateContext())
            {
                try
                {
                    var ids = ctx.Database.SqlQuery<int>("SELECT DescriptionID FROM dbo.TestingFaultDescription WHERE TMTID = @p0", tmtId).ToList();
                    if (!ids.Any())
                    {
                        var one = ctx.Database.SqlQuery<int?>("SELECT DescriptionID FROM dbo.TechnicalMapTesting WHERE TMTID = @p0", tmtId).FirstOrDefault();
                        if (one.HasValue && one.Value != 0) ids.Add(one.Value);
                    }
                    var texts = ctx.Description.Where(d => ids.Contains(d.DescriptionID)).Select(d => d.DescriptionText).Where(x => !string.IsNullOrEmpty(x)).ToList();
                    return string.Join(", ", texts);
                }
                catch { }
            }
            return "";
        }

        /// <summary>Тексты комментариев инспекции по записи Error.</summary>
        public static string GetInspectionCommentTexts(int errorId)
        {
            using (var ctx = ConnectionHelper.CreateContext())
            {
                var inspDescriptions = ctx.Inspection
                    .Where(i => i.ErrorID == errorId)
                    .Select(i => i.Description)
                    .Where(d => d != null)
                    .Select(d => d.DescriptionText)
                    .Where(t => !string.IsNullOrEmpty(t))
                    .Distinct()
                    .ToList();
                return string.Join(", ", inspDescriptions);
            }
        }
    }
}
