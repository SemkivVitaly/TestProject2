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

        #region Admin shipment without testing/QC

        /// <summary>Итог операции «отгрузка без тестирования и контроля».</summary>
        public struct SkipTestingResult
        {
            public readonly int Saved;
            public readonly int AlreadyShipped;
            public readonly int NotEligible;
            public SkipTestingResult(int saved, int alreadyShipped, int notEligible)
            { Saved = saved; AlreadyShipped = alreadyShipped; NotEligible = notEligible; }
        }

        /// <summary>
        /// Админская отгрузка акта на склад «После тестирования» без требования прохождения
        /// тестирования и контроля качества. Пропускает изолированные продукты и уже отгруженные.
        /// Обязательна <paramref name="reason"/> (минимум 3 символа) — пишется в журнал
        /// <c>dbo.ShipmentWithoutTesting</c> вместе с UserID и меткой времени.
        /// </summary>
        public static SkipTestingResult ShipActWithoutTesting(string actNumber, string reason, int userId)
        {
            if (string.IsNullOrWhiteSpace(actNumber)) throw new ArgumentNullException(nameof(actNumber));
            if (reason == null) throw new ArgumentNullException(nameof(reason));
            string trimmedReason = reason.Trim();
            if (trimmedReason.Length < 3)
                throw new ArgumentException("Причина должна содержать минимум 3 символа.", nameof(reason));
            if (trimmedReason.Length > 1000) trimmedReason = trimmedReason.Substring(0, 1000);

            string act = actNumber.Trim();
            return DbOperation.Run(ctx =>
            {
                var utc = DateTime.UtcNow;
                int saved = 0, alreadyShipped = 0, notEligible = 0;

                using (var tx = ctx.Database.BeginTransaction())
                {
                    var actEntity = ctx.Act.FirstOrDefault(a => a.ActNumber == act);
                    if (actEntity == null)
                        throw new InvalidOperationException($"Акт «{act}» не найден.");

                    var products = ctx.Product
                        .Where(p => p.ActID == actEntity.ActID)
                        .ToList();

                    // Находим id «изолированных» продуктов по последней инспекции
                    // (ResultText LIKE '%Изолировано%'). Такие продукты отгружать нельзя.
                    var isolatedIds = new HashSet<int>(
                        ctx.Error
                            .Include("TechnicalMapFull.Product")
                            .Include("Inspection.ResultTable")
                            .Where(er => er.TechnicalMapFull != null
                                && er.TechnicalMapFull.Product != null
                                && er.TechnicalMapFull.Product.ActID == actEntity.ActID)
                            .ToList()
                            .Where(er =>
                            {
                                var lastInsp = er.Inspection?
                                    .Where(i => i.ResultTable != null && i.ResultTable.ResultText != null)
                                    .OrderByDescending(i => i.InspectionID)
                                    .FirstOrDefault();
                                string rt = lastInsp?.ResultTable?.ResultText ?? "";
                                return rt.IndexOf("Изолировано", StringComparison.OrdinalIgnoreCase) >= 0;
                            })
                            .Select(er => er.TechnicalMapFull.Product.ProductID));

                    foreach (var p in products)
                    {
                        if (isolatedIds.Contains(p.ProductID)) { notEligible++; continue; }

                        if (p.PostTestingWarehouseAt != null) { alreadyShipped++; continue; }

                        p.QualityControlPassed = true;
                        p.QualityControlPassedUtc = utc;
                        p.QualityControlByUserID = userId;
                        p.PostTestingWarehouseAt = utc;
                        p.PostTestingWarehouseByUserID = userId;
                        saved++;
                    }
                    ctx.SaveChanges();

                    // Запись в журнал — через параметризованный raw SQL, чтобы не менять EDMX.
                    ctx.Database.ExecuteSqlCommand(
                        "INSERT INTO dbo.ShipmentWithoutTesting (ActID, UserID, Reason, ShipmentUtc, ProductCount) " +
                        "VALUES (@p0, @p1, @p2, @p3, @p4)",
                        actEntity.ActID, userId, trimmedReason, utc, saved);

                    tx.Commit();
                }
                return new SkipTestingResult(saved, alreadyShipped, notEligible);
            }, "ProductLifecycleService.ShipActWithoutTesting");
        }

        #endregion

        #region Auto-testing completion

        /// <summary>
        /// Продукты, у которых последняя запись <see cref="TechnicalMapTesting"/> имеет <c>InProgress=true</c>.
        /// </summary>
        public static HashSet<int> GetProductIdsWhereLatestTestingIsInProgress(IEnumerable<int> productIds)
        {
            var idList = productIds == null ? new List<int>() : productIds.Distinct().ToList();
            if (idList.Count == 0) return new HashSet<int>();

            return DbOperation.Run(ctx =>
            {
                var result = new HashSet<int>();
                foreach (int pid in idList)
                {
                    var full = ProductLifecycleValidation.GetCanonicalTechnicalMapFullForTesting(ctx, pid);
                    if (full == null) continue;
                    var latest = ctx.TechnicalMapTesting.AsNoTracking()
                        .Where(t => t.TMID == full.TMID)
                        .OrderByDescending(t => t.TMTID)
                        .FirstOrDefault();
                    if (latest != null && latest.InProgress)
                        result.Add(pid);
                }
                return result;
            }, "ProductLifecycleService.GetProductIdsWhereLatestTestingIsInProgress");
        }

        /// <summary>
        /// Возвращает true, если продукт в активном тестировании у другого пользователя (не <paramref name="currentUserId"/>).
        /// </summary>
        public static bool IsTestingInProgressHeldByAnotherUser(int productId, int currentUserId, out string holderUserName)
        {
            var r = DbOperation.Run(ctx =>
            {
                var full = ProductLifecycleValidation.GetCanonicalTechnicalMapFullForTesting(ctx, productId);
                if (full == null) return (false, (string)null);
                var latest = ctx.TechnicalMapTesting.AsNoTracking()
                    .Where(t => t.TMID == full.TMID)
                    .OrderByDescending(t => t.TMTID)
                    .FirstOrDefault();
                if (latest == null || !latest.InProgress) return (false, (string)null);
                if (latest.UserID == currentUserId) return (false, (string)null);
                var u = ctx.UsersProfile.AsNoTracking().FirstOrDefault(p => p.UserID == latest.UserID);
                string name = u != null ? (u.UserName ?? "") : "";
                if (string.IsNullOrWhiteSpace(name))
                    name = "пользователь ID " + latest.UserID;
                return (true, name);
            }, "ProductLifecycleService.IsTestingInProgressHeldByAnotherUser");
            holderUserName = r.Item2;
            return r.Item1;
        }

        /// <summary>
        /// Проверка перед автотестом (полётники, кросс-платы, Bridge): нельзя, если тест уже успешно завершён;
        /// нельзя, если сеанс «в работе» у другого пользователя (свой незавершённый сеанс — можно продолжить).
        /// </summary>
        public static bool TryValidateAutoTestAccess(int productId, int currentUserId, out string blockMessage)
        {
            var r = DbOperation.Run(ctx =>
            {
                if (ProductLifecycleValidation.LatestTestingSucceeded(ctx, productId))
                {
                    return (false,
                        "Этот продукт уже успешно прошёл тестирование (статус «Готово»). Повторный запуск недоступен.");
                }

                var full = ProductLifecycleValidation.GetCanonicalTechnicalMapFullForTesting(ctx, productId);
                if (full != null)
                {
                    var latest = ctx.TechnicalMapTesting.AsNoTracking()
                        .Where(t => t.TMID == full.TMID)
                        .OrderByDescending(t => t.TMTID)
                        .FirstOrDefault();
                    if (latest != null && latest.InProgress && latest.UserID != 0 && latest.UserID != currentUserId)
                    {
                        var u = ctx.UsersProfile.AsNoTracking().FirstOrDefault(p => p.UserID == latest.UserID);
                        string name = u != null ? (u.UserName ?? "") : "";
                        if (string.IsNullOrWhiteSpace(name))
                            name = "пользователь ID " + latest.UserID;
                        return (false, $"Продукт уже в тестировании у пользователя «{name}».");
                    }
                }

                return (true, (string)null);
            }, "ProductLifecycleService.TryValidateAutoTestAccess");
            blockMessage = r.Item2;
            return r.Item1;
        }

        /// <summary>
        /// Отмечает тестирование продукта как «В работе» — создаёт запись
        /// <see cref="TechnicalMapTesting"/> с <c>InProgress=true</c>. Предназначен для
        /// момента старта любого автоматического теста (кросс-плата, полётник, Bridge),
        /// чтобы в форме сотрудника/контроле продукт сразу показывался «в работе» у
        /// конкретного пользователя и его нельзя было взять повторно.
        ///
        /// Идемпотентно: если уже есть успешная запись — ничего не делает; если есть
        /// активный сеанс — не дублирует; в остальных случаях создаёт новый сеанс.
        /// </summary>
        /// <returns>true — запись уже существует или создана; false — продукт не найден.</returns>
        public static bool MarkTestingInProgress(int productId, int userId)
        {
            return DbOperation.Run(ctx =>
            {
                var full = ProductLifecycleValidation.GetCanonicalTechnicalMapFullForTesting(ctx, productId);
                if (full == null)
                {
                    if (!ctx.Product.Any(p => p.ProductID == productId)) return false;
                    full = new TechnicalMapFull { ProductID = productId, Inspection = false };
                    ctx.TechnicalMapFull.Add(full);
                    ctx.SaveChanges();
                }

                var latest = ctx.TechnicalMapTesting
                    .Where(t => t.TMID == full.TMID)
                    .OrderByDescending(t => t.TMTID)
                    .FirstOrDefault();

                // Уже есть активный сеанс у этого же (или любого) пользователя — повторно не создаём.
                if (latest != null && latest.InProgress) return true;

                // Уже есть успешная запись — повторное тестирование не блокируем, но и не ломаем статус.
                if (latest != null && latest.IsReadt && !latest.Fault) return true;

                var now = DateTime.Now;
                var tst = new TechnicalMapTesting
                {
                    TMID = full.TMID,
                    UserID = userId,
                    Date = now,
                    TimeStart = now.TimeOfDay,
                    TimeEnd = now.TimeOfDay,
                    InProgress = true,
                    IsReadt = false,
                    Fault = false
                };
                ctx.TechnicalMapTesting.Add(tst);
                ctx.SaveChanges();
                return true;
            }, "ProductLifecycleService.MarkTestingInProgress");
        }

        /// <summary>
        /// Создаёт (или обновляет текущий <see cref="TechnicalMapTesting"/> с IsReadt=true, чтобы продукт
        /// отразился в списках контроля качества и отгрузки. Безопасно вызывать многократно —
        /// если уже есть успешная запись, повторно не создаёт.
        /// </summary>
        /// <returns>true — была создана или обновлена запись; false — продукт не найден/не привязан.</returns>
        public static bool RecordSuccessfulAutoTest(int productId, int userId)
        {
            return DbOperation.Run(ctx =>
            {
                var full = ProductLifecycleValidation.GetCanonicalTechnicalMapFullForTesting(ctx, productId);
                if (full == null)
                {
                    // Нужен TechnicalMapFull — иначе TechnicalMapTesting некуда привязать.
                    if (!ctx.Product.Any(p => p.ProductID == productId)) return false;
                    full = new TechnicalMapFull { ProductID = productId, Inspection = false };
                    ctx.TechnicalMapFull.Add(full);
                    ctx.SaveChanges();
                }

                var now = DateTime.Now;
                var latest = ctx.TechnicalMapTesting
                    .Where(t => t.TMID == full.TMID)
                    .OrderByDescending(t => t.TMTID)
                    .FirstOrDefault();

                if (latest != null && latest.IsReadt && !latest.Fault)
                {
                    // Уже успешно — ничего не делаем, избегаем дублей.
                    return true;
                }

                if (latest != null && latest.InProgress)
                {
                    // Был активен сеанс тестирования (например, после ручной разблокировки) —
                    // закрываем его успешно.
                    latest.InProgress = false;
                    latest.IsReadt = true;
                    latest.Fault = false;
                    latest.Date = now;
                    latest.UserID = userId;
                    if (latest.TimeStart == TimeSpan.Zero) latest.TimeStart = now.TimeOfDay;
                    latest.TimeEnd = now.TimeOfDay;
                    ctx.SaveChanges();
                    return true;
                }

                var tst = new TechnicalMapTesting
                {
                    TMID = full.TMID,
                    UserID = userId,
                    Date = now,
                    TimeStart = now.TimeOfDay,
                    TimeEnd = now.TimeOfDay,
                    InProgress = false,
                    IsReadt = true,
                    Fault = false
                };
                ctx.TechnicalMapTesting.Add(tst);
                ctx.SaveChanges();
                return true;
            }, "ProductLifecycleService.RecordSuccessfulAutoTest");
        }

        /// <summary>
        /// Зафиксировать неуспешный автоматический тест (брак / «Неисправность»). Создаёт
        /// завершённую запись TechnicalMapTesting с Fault=true, чтобы продукт отразился корректно
        /// в отчётах и не висел в «В работе». Если уже есть активная запись — закрывает её.
        /// </summary>
        public static bool RecordFailedAutoTest(int productId, int userId, int? descriptionId = null)
        {
            return DbOperation.Run(ctx =>
            {
                var full = ProductLifecycleValidation.GetCanonicalTechnicalMapFullForTesting(ctx, productId);
                if (full == null)
                {
                    if (!ctx.Product.Any(p => p.ProductID == productId)) return false;
                    full = new TechnicalMapFull { ProductID = productId, Inspection = false };
                    ctx.TechnicalMapFull.Add(full);
                    ctx.SaveChanges();
                }

                var now = DateTime.Now;
                var latest = ctx.TechnicalMapTesting
                    .Where(t => t.TMID == full.TMID)
                    .OrderByDescending(t => t.TMTID)
                    .FirstOrDefault();

                if (latest != null && latest.InProgress)
                {
                    latest.InProgress = false;
                    latest.IsReadt = false;
                    latest.Fault = true;
                    latest.Date = now;
                    latest.UserID = userId;
                    if (latest.TimeStart == TimeSpan.Zero) latest.TimeStart = now.TimeOfDay;
                    latest.TimeEnd = now.TimeOfDay;
                    if (descriptionId.HasValue) latest.DescriptionID = descriptionId.Value;
                    ctx.SaveChanges();
                    return true;
                }

                var tst = new TechnicalMapTesting
                {
                    TMID = full.TMID,
                    UserID = userId,
                    Date = now,
                    TimeStart = now.TimeOfDay,
                    TimeEnd = now.TimeOfDay,
                    InProgress = false,
                    IsReadt = false,
                    Fault = true,
                    DescriptionID = descriptionId
                };
                ctx.TechnicalMapTesting.Add(tst);
                ctx.SaveChanges();
                return true;
            }, "ProductLifecycleService.RecordFailedAutoTest");
        }

        #endregion
    }
}
