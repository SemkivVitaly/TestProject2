using System.Configuration;

namespace SaveData1.Helpers
{
    /// <summary>Соответствие Bridge ↔ запись в <c>ProducType.TypeName</c> (свои серийные номеры, не кросс-платы).</summary>
    public static class BridgeDbHelper
    {
        /// <summary>Значение по умолчанию, если в App.config не задано <c>BridgeProductTypeName</c>.</summary>
        public const string DefaultBridgeProductTypeName = "Bridge";

        /// <summary>Имя типа продукта в БД для серийных номеров на форме «Тестирование Bridge».</summary>
        public static string GetBridgeProductTypeName()
        {
            string s = ConfigurationManager.AppSettings["BridgeProductTypeName"];
            return string.IsNullOrWhiteSpace(s) ? DefaultBridgeProductTypeName : s.Trim();
        }
    }
}
