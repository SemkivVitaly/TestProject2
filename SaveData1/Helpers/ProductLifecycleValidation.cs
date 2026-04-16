using System.Data.Entity;
using System.Linq;
using SaveData1.Entity;

namespace SaveData1.Helpers
{
    /// <summary>Проверки перед записью этапов «контроль» / «на склад после теста» (согласованность с техкартой).</summary>
    public static class ProductLifecycleValidation
    {
        /// <summary>Последняя техкарта: сборка готова, не инспекция, последний тест успешен (IsReadt и не Fault).</summary>
        public static bool LatestTestingSucceeded(SaveDataEntities2 ctx, int productId)
        {
            var full = ctx.TechnicalMapFull
                .Include("TechnicalMapAssembly")
                .Include("TechnicalMapTesting")
                .Where(f => f.ProductID == productId)
                .OrderByDescending(f => f.TMID)
                .FirstOrDefault();

            if (full == null || full.Inspection)
                return false;
            if (full.TechnicalMapAssembly == null || !full.TechnicalMapAssembly.Any(a => a.IsReady))
                return false;

            var tst = full.TechnicalMapTesting != null && full.TechnicalMapTesting.Count > 0
                ? full.TechnicalMapTesting.OrderByDescending(t => t.TMTID).First()
                : null;

            return tst != null && tst.IsReadt && !tst.Fault;
        }
    }
}
