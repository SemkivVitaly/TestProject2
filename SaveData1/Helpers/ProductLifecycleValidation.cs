using System;
using System.Data.Entity;
using System.Linq;
using SaveData1.CrossPlateTesting.Services;
using SaveData1.Entity;

namespace SaveData1.Helpers
{
    /// <summary>Проверки перед записью этапов «контроль» / «на склад после теста» (согласованность с техкартой).</summary>
    public static class ProductLifecycleValidation
    {
        /// <summary>Тип продукта «Полетники» в <c>ProducType.TypeName</c>.</summary>
        public const string PolletnikiProductTypeName = "Полетники";

        /// <summary>Варианты <c>ProducType.TypeName</c> для полётников в БД (множ. и ед. число).</summary>
        public static readonly string[] PolletnikProducTypeNames = { PolletnikiProductTypeName, "Полетник" };

        /// <summary>Продукт закрыт для гридов сборки/тестирования сотрудника (пройден контроль или передан на склад «после теста»).</summary>
        public static bool IsProductClosedForEmployeeWorkshop(bool qualityControlPassed, DateTime? postTestingWarehouseAt)
        {
            return qualityControlPassed || postTestingWarehouseAt != null;
        }

        /// <summary>Полётники, кросс-платы и Bridge не имеют этапа <see cref="TechnicalMapAssembly"/> — проверки «готовой сборки» для них не применяются.</summary>
        public static bool ProductTypeHasNoAssemblyWorkflow(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName)) return false;
            string t = typeName.Trim();
            foreach (var n in PolletnikProducTypeNames)
            {
                if (string.Equals(t, n, StringComparison.OrdinalIgnoreCase)) return true;
            }
            if (string.Equals(t, CrossPlateDbHelper.CrossProductTypeName, StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(t, BridgeDbHelper.GetBridgeProductTypeName(), StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>
        /// Техкарта для записи автотеста: для типов без сборки — последняя по TMID;
        /// иначе последняя по TMID среди карт с готовой сборкой, либо последняя по TMID (до сборки).
        /// Используется в ProductLifecycleService.
        /// </summary>
        public static TechnicalMapFull GetCanonicalTechnicalMapFullForTesting(SaveDataEntities2 ctx, int productId)
        {
            string typeName = ctx.Product.AsNoTracking()
                .Where(p => p.ProductID == productId)
                .Select(p => p.ProducType.TypeName)
                .FirstOrDefault();

            if (!ProductTypeHasNoAssemblyWorkflow(typeName))
            {
                var full = ctx.TechnicalMapFull
                    .Where(f => f.ProductID == productId)
                    .Where(f => f.TechnicalMapAssembly.Any(a => a.IsReady))
                    .OrderByDescending(f => f.TMID)
                    .FirstOrDefault();
                if (full != null) return full;
            }
            return ctx.TechnicalMapFull
                .Where(f => f.ProductID == productId)
                .OrderByDescending(f => f.TMID)
                .FirstOrDefault();
        }

        /// <summary>
        /// Успешное тестирование для контроля/отгрузки: последняя по TMTID запись <see cref="TechnicalMapTesting"/>
        /// по продукту (не «в работе», не брак), техкарта не на инспекции; для типов со сборкой — готовая сборка на этом TMID;
        /// для полётников / кросс-плат / Bridge сборка не требуется. Иначе — успех по последней <see cref="TechnicalMatFlight"/> с Test_Pass.
        /// </summary>
        public static bool LatestTestingSucceeded(SaveDataEntities2 ctx, int productId)
        {
            string typeName = ctx.Product.AsNoTracking()
                .Where(p => p.ProductID == productId)
                .Select(p => p.ProducType.TypeName)
                .FirstOrDefault();
            bool noAsm = ProductTypeHasNoAssemblyWorkflow(typeName);

            var latestTst = ctx.TechnicalMapTesting.AsNoTracking()
                .Include("TechnicalMapFull")
                .Where(t => t.TechnicalMapFull.ProductID == productId)
                .OrderByDescending(t => t.TMTID)
                .FirstOrDefault();

            if (latestTst != null)
            {
                if (latestTst.InProgress || latestTst.Fault)
                    return false;
                var f = latestTst.TechnicalMapFull;
                if (f != null && f.Inspection)
                    return false;
                if (latestTst.IsReadt && !latestTst.Fault
                    && (noAsm || ctx.TechnicalMapAssembly.AsNoTracking().Any(a => a.TMID == latestTst.TMID && a.IsReady)))
                    return true;
            }

            var flight = ctx.TechnicalMatFlight.AsNoTracking()
                .Where(t => t.ProductID == productId)
                .OrderByDescending(t => t.TFlightID)
                .FirstOrDefault();
            return flight != null && flight.Test_Pass;
        }
    }
}
