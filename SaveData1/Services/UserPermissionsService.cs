using System;
using System.Data.Entity;
using System.Linq;
using SaveData1.Entity;
using SaveData1.Helpers;

namespace SaveData1.Services
{
    /// <summary>Загрузка пользователя + расчёт флагов прав. Единый источник истины
    /// для <see cref="SaveData1.LoginForm"/> и <see cref="SaveData1.EmployeeForm"/>.</summary>
    public static class UserPermissionsService
    {
        public const string PermAssembly = "Сборщик";
        public const string PermTesting = "Тестировщик";
        public const string PermInspection = "Инспектор";
        public const string PermControl = "Контроль";
        public const string PermAdmin = "Администратор";

        public const string RoleAdmin = "Admin";
        public const string RoleStorage = "Storage";

        /// <summary>Загрузить пользователя по логину с ролью и правами (AsNoTracking). null — если не найден.</summary>
        public static UsersProfile FindByLogin(string login)
        {
            if (string.IsNullOrWhiteSpace(login)) return null;
            string normalized = login.Trim();
            return DbOperation.Run(ctx => ctx.UsersProfile
                .AsNoTracking()
                .Include("Role")
                .Include("UserWithPermissions.Permissions")
                .FirstOrDefault(u => u.UserLogin == normalized),
                nameof(FindByLogin));
        }

        /// <summary>Перезагрузить пользователя по ID (AsNoTracking). null — если не найден.</summary>
        public static UsersProfile FindById(int userId)
        {
            return DbOperation.Run(ctx => ctx.UsersProfile
                .AsNoTracking()
                .Include("Role")
                .Include("UserWithPermissions.Permissions")
                .FirstOrDefault(u => u.UserID == userId),
                nameof(FindById));
        }

        /// <summary>Снимок прав пользователя. Удобно передавать вместе с пользователем и не пересчитывать в нескольких местах.</summary>
        public static UserFlags GetFlags(UsersProfile user)
        {
            if (user == null) return default(UserFlags);

            bool hasAssembly = false, hasTesting = false, hasInspection = false, hasControl = false, hasAdminPerm = false;
            if (user.UserWithPermissions != null)
            {
                foreach (var uwp in user.UserWithPermissions)
                {
                    var name = uwp?.Permissions?.PermissionsName;
                    if (name == null) continue;
                    if (name == PermAssembly) hasAssembly = true;
                    else if (name == PermTesting) hasTesting = true;
                    else if (name == PermInspection) hasInspection = true;
                    else if (name == PermControl) hasControl = true;
                    else if (name == PermAdmin) hasAdminPerm = true;
                }
            }
            string role = user.Role != null ? user.Role.RoleName : null;
            bool isAdmin = role == RoleAdmin;
            bool isStorage = role == RoleStorage;
            return new UserFlags
            {
                HasAssembly = hasAssembly,
                HasTesting = hasTesting,
                HasInspection = hasInspection,
                HasControl = hasControl,
                HasAdminPermission = hasAdminPerm,
                IsAdmin = isAdmin,
                IsStorage = isStorage
            };
        }
    }

    /// <summary>Плоский набор прав/ролевых признаков пользователя.</summary>
    public struct UserFlags
    {
        public bool HasAssembly;
        public bool HasTesting;
        public bool HasInspection;
        public bool HasControl;
        public bool HasAdminPermission;
        public bool IsAdmin;
        public bool IsStorage;

        /// <summary>Может ли пользоваться основной рабочей формой сотрудника.</summary>
        public bool CanUseEmployeeForm => HasAssembly || HasTesting || HasInspection || HasControl || IsAdmin || HasAdminPermission;
    }
}
