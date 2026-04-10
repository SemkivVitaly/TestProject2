using System;
using System.Data.Entity.Core.EntityClient;
using System.Data.SqlClient;
using System.Security.Cryptography;
using SaveData1.Entity;
using SaveData1.Helpers;
using SaveData1.Properties;

namespace SaveData1
{
    /// <summary>Строка подключения к БД и создание контекста EF.</summary>
    public static class ConnectionHelper
    {
        /// <summary>Формирование строки подключения из настроек приложения.</summary>
        public static string GetConnectionString()
        {
            var settings = Settings.Default;
            string server = string.IsNullOrWhiteSpace(settings.DbServer) ? "MSI" : settings.DbServer;

            var sqlBuilder = new SqlConnectionStringBuilder
            {
                DataSource = server,
                InitialCatalog = "SaveData",
                MultipleActiveResultSets = true,
                ApplicationName = "EntityFramework",
                Encrypt = false
            };

            if (settings.DbUseSqlAuth && !string.IsNullOrEmpty(settings.DbLogin))
            {
                sqlBuilder.IntegratedSecurity = false;
                sqlBuilder.UserID = settings.DbLogin;
                sqlBuilder.Password = LocalSecretProtector.UnprotectOrPlain(settings.DbPassword ?? "");
            }
            else
            {
                sqlBuilder.IntegratedSecurity = true;
            }

            var entityBuilder = new EntityConnectionStringBuilder
            {
                Provider = "System.Data.SqlClient",
                ProviderConnectionString = sqlBuilder.ToString(),
                Metadata = "res://*/Entity.Model1.csdl|res://*/Entity.Model1.ssdl|res://*/Entity.Model1.msl"
            };

            return entityBuilder.ToString();
        }

        /// <summary>Создание контекста БД.</summary>
        public static SaveDataEntities2 CreateContext()
        {
            return new SaveDataEntities2(GetConnectionString());
        }

        /// <summary>Проверка подключения к серверу БД.</summary>
        public static bool TestConnection(string server, bool useSqlAuth, string login, string password)
        {
            var sqlBuilder = new SqlConnectionStringBuilder
            {
                DataSource = server,
                InitialCatalog = "SaveData",
                ConnectTimeout = 5,
                Encrypt = false
            };

            if (useSqlAuth && !string.IsNullOrEmpty(login))
            {
                sqlBuilder.IntegratedSecurity = false;
                sqlBuilder.UserID = login;
                sqlBuilder.Password = password ?? "";
            }
            else
            {
                sqlBuilder.IntegratedSecurity = true;
            }

            using (var conn = new SqlConnection(sqlBuilder.ToString()))
            {
                conn.Open();
                return true;
            }
        }
    }

    /// <summary>PBKDF2-хэш паролей пользователей приложения в БД (UsersProfile.UserPassword). Старые записи в открытом виде при входе пересохраняются в хэш (см. LoginForm).</summary>
    public static class PasswordHasher
    {
        private const string Prefix = "pbkdf2$v1$";
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 50000;

        public static string HashPassword(string plainText)
        {
            if (plainText == null) throw new ArgumentNullException(nameof(plainText));
            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(salt);
            byte[] key = Pbkdf2(plainText, salt, Iterations, KeySize);
            return Prefix + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(key);
        }

        public static bool Verify(string plainText, string stored)
        {
            if (string.IsNullOrEmpty(stored) || plainText == null) return false;
            if (!stored.StartsWith(Prefix, StringComparison.Ordinal))
                return string.Equals(plainText, stored, StringComparison.Ordinal);

            string rest = stored.Substring(Prefix.Length);
            int dollar = rest.IndexOf('$');
            if (dollar <= 0 || dollar >= rest.Length - 1) return false;
            try
            {
                byte[] salt = Convert.FromBase64String(rest.Substring(0, dollar));
                byte[] expected = Convert.FromBase64String(rest.Substring(dollar + 1));
                if (expected.Length != KeySize) return false;
                byte[] actual = Pbkdf2(plainText, salt, Iterations, KeySize);
                return FixedTimeEquals(actual, expected);
            }
            catch
            {
                return false;
            }
        }

        public static bool IsHashedFormat(string stored) =>
            !string.IsNullOrEmpty(stored) && stored.StartsWith(Prefix, StringComparison.Ordinal);

        private static byte[] Pbkdf2(string password, byte[] salt, int iterations, int keyBytes)
        {
            using (var derive = new Rfc2898DeriveBytes(password, salt, iterations))
                return derive.GetBytes(keyBytes);
        }

        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
