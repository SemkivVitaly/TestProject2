using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using SaveData1.Entity;
using SaveData1.Helpers;

namespace SaveData1.Services
{
    /// <summary>Бизнес-операции жизненного цикла продукта (контроль качества, отгрузка на склад после теста,
    /// ручные разблокировки). Использует <see cref="ProductLifecycleValidation"/> для проверок.</summary>
    public static class ProductLifecycleService
    {
        /// <summary>Сколько часов действует ручная разблокировка (сборка/тест).</summary>
        public const int ManualUnlockTtlHours = 12;

        #region Quality Control

        /// <summary>Отметить набор продуктов прошедшими контроль качества. Возвращает (saved, skipped).
        /// Пропускает записи, не соответствующие условиям (неверный акт, уже на складе, неуспешный тест).</summary>
        public static QcResult MarkQualityControlPassed(string actNumber, IEnumerable<int> productIds, int userId)
        {
            if (string.IsNullOrWhiteSpace(actNumber)) throw new ArgumentNullException(nameof(actNumber));
            if (productIds == null) throw new ArgumentNullException(nameof(productIds));
            var pidList = productIds.Distinct().ToList();
            if (pidList.Count == 0) return new QcResult(0, 0);
            string act = actNumber.Trim();

            return DbOperation.Run(ctx =>
            {
                int saved = 0, skipped = 0;
                using (var tx = ctx.Database.BeginTransaction())
                {
                    var utc = DateTime.UtcNow;
                    foreach (int pid in pidList)
                    {
                        var p = ctx.Product.Include(x => x.Act).FirstOrDefault(x => x.ProductID == pid);
                        if (p == null || p.ActID == null || p.Act == null || p.Act.ActNumber != act) { skipped++; continue; }
                        if (p.PostTestingWarehouseAt != null) { skipped++; continue; }
                        if (!ProductLifecycleValidation.LatestTestingSucceeded(ctx, pid)) { skipped++; continue; }

                        p.QualityControlPassed = true;
                        p.QualityControlPassedUtc = utc;
                        p.QualityControlByUserID = userId;
                        saved++;
                    }
                    ctx.SaveChanges();
                    tx.Commit();
                }
                return new QcResult(saved, skipped);
            }, "ProductLifecycleService.MarkQualityControlPassed");
        }

        #endregion

        #region Ship to warehouse after testing

        /// <summary>Передача продуктов на склад после тестирования. Пропускает записи, не прошедшие контроль
        /// или уже отгруженные или с неуспешным тестом.</summary>
        public static QcResult ShipToPostTestingWarehouse(string actNumber, IEnumerable<int> productIds, int userId)
        {
            if (string.IsNullOrWhiteSpace(actNumber)) throw new ArgumentNullException(nameof(actNumber));
            if (productIds == null) throw new ArgumentNullException(nameof(productIds));
            var pidList = productIds.Distinct().ToList();
            if (pidList.Count == 0) return new QcResult(0, 0);
            string act = actNumber.Trim();

            return DbOperation.Run(ctx =>
            {
                int saved = 0, skipped = 0;
                using (var tx = ctx.Database.BeginTransaction())
                {
                    var utc = DateTime.UtcNow;
                    foreach (int pid in pidList)
                    {
                        var p = ctx.Product.Include(x => x.Act).FirstOrDefault(x => x.ProductID == pid);
                        if (p == null || p.ActID == null || p.Act == null || p.Act.ActNumber != act) { skipped++; continue; }
                        if (!p.QualityControlPassed || p.PostTestingWarehouseAt != null) { skipped++; continue; }
                        if (!ProductLifecycleValidation.LatestTestingSucceeded(ctx, pid)) { skipped++; continue; }

                        p.PostTestingWarehouseAt = utc;
                        p.PostTestingWarehouseByUserID = userId;
                        saved++;
                    }
                    ctx.SaveChanges();
                    tx.Commit();
                }
                return new QcResult(saved, skipped);
            }, "ProductLifecycleService.ShipToPostTestingWarehouse");
        }

        #endregion

        #region Manual unlocks (assembly/testing)

        public enum UnlockScope { Assembly, Testing }

        /// <summary>Установить ручную разблокировку для продукта. TTL — 12 часов, проверяется в <see cref="IsUnlockActive"/>.</summary>
        public static void SetManualUnlock(int productId, UnlockScope scope, int userId)
        {
            DbOperation.Execute(ctx =>
            {
                var p = ctx.Product.Find(productId);
                if (p == null) return;
                var utc = DateTime.UtcNow;
                if (scope == UnlockScope.Assembly)
                {
                    p.AssemblyManualUnlockByUserID = userId;
                    p.AssemblyManualUnlockUtc = utc;
                }
                else
                {
                    p.TestingManualUnlockByUserID = userId;
                    p.TestingManualUnlockUtc = utc;
                }
                ctx.SaveChanges();
            }, "ProductLifecycleService.SetManualUnlock");
        }

        /// <summary>Сбросить ручную разблокировку после успешной сессии.</summary>
        public static void ClearManualUnlock(int productId, UnlockScope scope)
        {
            DbOperation.Execute(ctx =>
            {
                var p = ctx.Product.Find(productId);
                if (p == null) return;
                if (scope == UnlockScope.Assembly)
                {
                    p.AssemblyManualUnlockByUserID = null;
                    p.AssemblyManualUnlockUtc = null;
                }
                else
                {
                    p.TestingManualUnlockByUserID = null;
                    p.TestingManualUnlockUtc = null;
                }
                ctx.SaveChanges();
            }, "ProductLifecycleService.ClearManualUnlock");
        }

        /// <summary>Активна ли разблокировка (по UTC-метке и TTL).</summary>
        public static bool IsUnlockActive(DateTime? unlockUtc)
        {
            if (unlockUtc == null) return false;
            return (DateTime.UtcNow - unlockUtc.Value).TotalHours <= ManualUnlockTtlHours;
        }

        #endregion

        /// <summary>Итог массовой операции lifecycle.</summary>
        public struct QcResult
        {
            public readonly int Saved;
            public readonly int Skipped;
            public QcResult(int saved, int skipped) { Saved = saved; Skipped = skipped; }
        }
    }
}
