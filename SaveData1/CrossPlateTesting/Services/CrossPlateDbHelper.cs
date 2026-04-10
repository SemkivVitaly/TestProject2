using System;
using System.Data.Entity;
using System.Linq;
using SaveData1.Entity;
using SaveData1.Helpers;

namespace SaveData1.CrossPlateTesting.Services
{
    public static class CrossPlateDbHelper
    {
        public const string CrossProductTypeName = "Кросс-плата";

        /// <summary>Текст описания для ярлыка несоответствия при нажатии «Неисправность» на стенде кросс-плат.</summary>
        public const string CrossPlateDefectDescriptionText = "Неисправность при тестировании кросс-платы";

        public static bool TryGetCrossProduct(SaveDataEntities2 ctx, int actId, string serial, out Product product)
        {
            product = ctx.Product.AsNoTracking()
                .Include(p => p.ProducType)
                .FirstOrDefault(p => p.ActID == actId
                    && p.ProductSerial == serial
                    && p.ProducType != null
                    && p.ProducType.TypeName == CrossProductTypeName);
            return product != null;
        }

        public static void RecordSuccess(SaveDataEntities2 ctx, int productId, int userId, out int tflightId)
        {
            var tflight = new TechnicalMatFlight
            {
                Date = DateTime.Now,
                ProductID = productId,
                UserID = userId,
                Test_Pass = true
            };
            ctx.TechnicalMatFlight.Add(tflight);
            ctx.SaveChanges();
            tflightId = tflight.TFlightID;
            ctx.TestFlight.Add(new TestFlight
            {
                TFlightID = tflightId,
                Stand = 0,
                Visual = true, Damage = true, FC1 = true, FC2 = true, C_5V_FC1 = true, C_5V_FC2 = true,
                FC_Test_Pass = true, Externa_Test_Pass = true, Long_Test_Pass = true,
                Result = "", Description = ""
            });
            ctx.SaveChanges();
        }

        public static int CreateFailedTestSession(SaveDataEntities2 ctx, int productId, int userId)
        {
            var tflight = new TechnicalMatFlight
            {
                Date = DateTime.Now,
                ProductID = productId,
                UserID = userId,
                Test_Pass = false
            };
            ctx.TechnicalMatFlight.Add(tflight);
            ctx.SaveChanges();
            return tflight.TFlightID;
        }

        /// <summary>
        /// Создаёт запись Error (ярлык несоответствия / акт несоответствия в БД), как при «В ремонт» в форме полётного теста.
        /// PlaceID = 3 (тестирование кросс-плат).
        /// </summary>
        public static int CreateNonConformityError(SaveDataEntities2 ctx, int productId)
        {
            int productTypeId = ctx.Product.Where(p => p.ProductID == productId).Select(p => p.TypeID).FirstOrDefault();
            if (productTypeId == 0)
                throw new InvalidOperationException("Продукт не найден или не задан тип (TypeID).");

            var full = ctx.TechnicalMapFull.FirstOrDefault(f => f.ProductID == productId);
            if (full == null)
            {
                full = new TechnicalMapFull { ProductID = productId, Inspection = false };
                ctx.TechnicalMapFull.Add(full);
                ctx.SaveChanges();
            }

            full.Inspection = true;

            const int testingPlaceId = 3;

            var err = new Error
            {
                ProductID = productId,
                PlaceID = testingPlaceId,
                TMID = full.TMID,
                Date = DateTime.Now,
                inProgress = false
            };
            ctx.Error.Add(err);
            ctx.SaveChanges();

            var desc = ctx.Description.FirstOrDefault(d => d.DescriptionText == CrossPlateDefectDescriptionText);
            if (desc == null)
            {
                desc = new Description
                {
                    DescriptionText = CrossPlateDefectDescriptionText,
                    TypeID = productTypeId
                };
                ctx.Description.Add(desc);
                ctx.SaveChanges();
            }

            FaultDescriptionHelper.SetErrorDescriptions(err.ErrorID, new[] { desc.DescriptionID });
            return err.ErrorID;
        }
    }
}
