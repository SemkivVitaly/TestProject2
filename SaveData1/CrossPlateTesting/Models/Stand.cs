using System;

namespace SaveData1.CrossPlateTesting.Models
{
    /// <summary>
    /// Стенд с информацией о Wi-Fi подключении
    /// </summary>
    public class Stand
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string WifiSsid { get; set; }
        public string WifiPassword { get; set; }
        public bool HasSavedCredentials { get; set; }
        /// <summary>Серийный номер тестируемого продукта. В режиме мониторинга стенды с пустым значением пропускаются.</summary>
        public string ProductSerialNumber { get; set; }

        public Stand()
        {
            Id = Guid.NewGuid().ToString("N");
            Name = "";
        }
    }
}
