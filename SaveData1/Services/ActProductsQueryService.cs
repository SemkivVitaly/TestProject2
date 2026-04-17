using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using SaveData1.Entity;
using SaveData1.Helpers;

namespace SaveData1.Services
{
    /// <summary>Проекционные запросы для гридов актов/продуктов. Использует <see cref="DbOperation"/> —
    /// ошибки/ретраи централизованы. Вызывающие формы получают DTO без трекинга EF и без Include-деревьев.</summary>
    public static class ActProductsQueryService
    {
        public sealed class ActListItem
        {
            public int ActID { get; set; }
            public string ActNumber { get; set; }
            public bool IsReady { get; set; }
            public int ProductCount { get; set; }

            public override string ToString() => ActNumber ?? "—";
        }

        public sealed class SimpleProductItem
        {
            public int ProductID { get; set; }
            public string SerialNumber { get; set; }
            public string Category { get; set; }
            public string CountryName { get; set; }
            public int? ActID { get; set; }
            public string ActNumber { get; set; }
            public bool QualityControlPassed { get; set; }
            public System.DateTime? PostTestingWarehouseAt { get; set; }
        }

        /// <summary>Список всех актов с количеством продуктов (без Include трекинга).</summary>
        public static List<ActListItem> GetAllActs(bool onlyReady = false)
        {
            return DbOperation.Run(ctx =>
            {
                var q = ctx.Act.AsNoTracking().AsQueryable();
                if (onlyReady) q = q.Where(a => a.IsReady);
                return q.Select(a => new ActListItem
                {
                    ActID = a.ActID,
                    ActNumber = a.ActNumber,
                    IsReady = a.IsReady,
                    ProductCount = a.Product.Count
                }).OrderBy(a => a.ActNumber).ToList();
            }, nameof(GetAllActs));
        }

        /// <summary>Готовые акты с фильтрацией по категории (TypeName) / категории+стране.</summary>
        public static List<ActListItem> GetReadyActsFiltered(string filterTypeName, int? filterTypeID, int? filterCountryID)
        {
            return DbOperation.Run(ctx =>
            {
                var query = ctx.Act.AsNoTracking().Where(a => a.IsReady);
                if (!string.IsNullOrEmpty(filterTypeName))
                {
                    query = query.Where(a => a.Product.Any(p => p.ProducType != null && p.ProducType.TypeName == filterTypeName));
                }
                else if (filterTypeID.HasValue)
                {
                    int typeId = filterTypeID.Value;
                    if (filterCountryID.HasValue)
                    {
                        int countryId = filterCountryID.Value;
                        query = query.Where(a => a.Product.Any(p => p.TypeID == typeId && p.ProducType != null && p.ProducType.CountryID == countryId));
                    }
                    else
                    {
                        query = query.Where(a => a.Product.Any(p => p.TypeID == typeId));
                    }
                }
                return query.Select(a => new ActListItem
                {
                    ActID = a.ActID,
                    ActNumber = a.ActNumber,
                    IsReady = a.IsReady,
                    ProductCount = a.Product.Count
                }).OrderBy(a => a.ActNumber).ToList();
            }, nameof(GetReadyActsFiltered));
        }

        /// <summary>Продукты без привязанного акта (для WarehouseForm).</summary>
        public static List<SimpleProductItem> GetUnassignedProducts()
        {
            return DbOperation.Run(ctx => ctx.Product.AsNoTracking()
                .Where(p => p.Act == null)
                .Select(p => new SimpleProductItem
                {
                    ProductID = p.ProductID,
                    SerialNumber = p.ProductSerial,
                    Category = p.ProducType != null ? p.ProducType.TypeName : "",
                    CountryName = p.ProducType != null && p.ProducType.Country != null ? p.ProducType.Country.CountryName : "",
                    ActID = p.ActID,
                    ActNumber = p.Act != null ? p.Act.ActNumber : null,
                    QualityControlPassed = p.QualityControlPassed,
                    PostTestingWarehouseAt = p.PostTestingWarehouseAt
                })
                .OrderBy(p => p.SerialNumber)
                .ToList(),
                nameof(GetUnassignedProducts));
        }

        /// <summary>Продукты выбранного акта (плоская проекция без Include).</summary>
        public static List<SimpleProductItem> GetProductsByAct(string actNumber)
        {
            if (string.IsNullOrWhiteSpace(actNumber)) return new List<SimpleProductItem>();
            string act = actNumber.Trim();
            return DbOperation.Run(ctx => ctx.Product.AsNoTracking()
                .Where(p => p.Act != null && p.Act.ActNumber == act)
                .Select(p => new SimpleProductItem
                {
                    ProductID = p.ProductID,
                    SerialNumber = p.ProductSerial,
                    Category = p.ProducType != null ? p.ProducType.TypeName : "",
                    CountryName = p.ProducType != null && p.ProducType.Country != null ? p.ProducType.Country.CountryName : "",
                    ActID = p.ActID,
                    ActNumber = p.Act != null ? p.Act.ActNumber : null,
                    QualityControlPassed = p.QualityControlPassed,
                    PostTestingWarehouseAt = p.PostTestingWarehouseAt
                })
                .OrderBy(p => p.SerialNumber)
                .ToList(),
                nameof(GetProductsByAct));
        }
    }
}
