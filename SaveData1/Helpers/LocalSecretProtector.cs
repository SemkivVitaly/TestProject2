using System;
using System.Security.Cryptography;
using System.Text;

namespace SaveData1.Helpers
{
    /// <summary>
    /// Локальные секреты (пароль SQL в настройках, OBS, Wi‑Fi в JSON конфигурации кросс‑платы):
    /// шифрование DPAPI для текущего пользователя Windows. Односторонний хэш здесь невозможен —
    /// приложению нужен исходный пароль для подключения. Пароли учётных записей приложения в БД
    /// хранятся отдельно через <see cref="PasswordHasher"/> (PBKDF2).
    /// </summary>
    public static class LocalSecretProtector
    {
        private const string Prefix = "dpapi$v1$";
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("SaveData1.LocalSecrets.v1");

        public static bool IsProtected(string value) =>
            !string.IsNullOrEmpty(value) && value.StartsWith(Prefix, StringComparison.Ordinal);

        /// <summary>Сохраняемое значение; пустая строка не меняется.</summary>
        public static string Protect(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;
            byte[] raw = Encoding.UTF8.GetBytes(plainText);
            byte[] enc = ProtectedData.Protect(raw, Entropy, DataProtectionScope.CurrentUser);
            return Prefix + Convert.ToBase64String(enc);
        }

        /// <summary>Расшифровка; строки без префикса возвращаются как есть (старые настройки в открытом виде).</summary>
        public static string UnprotectOrPlain(string stored)
        {
            if (string.IsNullOrEmpty(stored)) return stored;
            if (!IsProtected(stored)) return stored;
            try
            {
                string b64 = stored.Substring(Prefix.Length);
                byte[] enc = Convert.FromBase64String(b64);
                byte[] dec = ProtectedData.Unprotect(enc, Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(dec);
            }
            catch
            {
                return "";
            }
        }
    }
}
