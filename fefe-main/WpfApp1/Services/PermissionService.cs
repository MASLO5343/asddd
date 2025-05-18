// fefe-main/WpfApp1/Services/PermissionService.cs
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic; // Для HashSet и Dictionary
using WpfApp1.Constants;       // Для Permissions
using WpfApp1.Interfaces;

namespace WpfApp1.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly ILogger<PermissionService> _logger;

        // Словарь для хранения прав по ролям (сохранен из первой версии)
        // В будущем это можно загружать из БД или конфигурации
        // Используем обновленные константы прав, если они изменились (например, ViewLogs)
        private readonly Dictionary<string, HashSet<string>> _rolePermissions =
            new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase) // Сравнение без учета регистра
            {
                {
                    "Admin", new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        // Администратор может всё (добавляем все права из Permissions.cs)
                        Permissions.ViewUsers, Permissions.ManageUsers,
                        Permissions.ViewInventory, Permissions.ManageInventory,
                        Permissions.ViewTickets, Permissions.ManageTickets, Permissions.AssignTickets, Permissions.CommentTickets,
                        Permissions.AccessAdminSection,
                        Permissions.ViewSystemLogs, // Используем актуальное значение "Permissions.Logs.View" (было ViewLogs)
                        Permissions.ViewMonitoring, Permissions.ManageSettings
                        // Добавьте сюда Permissions.ManageLogs, если раскомментируете его в Permissions.cs
                    }
                },
                {
                    "Operator", new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        // Оператор (примерный набор прав из первой версии)
                        Permissions.ViewInventory,
                        Permissions.ViewTickets, Permissions.ManageTickets, Permissions.AssignTickets, Permissions.CommentTickets,
                        Permissions.ViewUsers,
                        Permissions.ViewSystemLogs // Например, оператор тоже может смотреть логи (было ViewLogs)
                    }
                },
                {
                    "User", new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        // Обычный пользователь (минимальные права из первой версии)
                        Permissions.ViewTickets, // Может смотреть свои заявки (потребуется доп. логика фильтрации)
                        Permissions.CommentTickets // Может комментировать свои заявки
                        // Permissions.CreateTicket // Возможно, стоит добавить отдельное право на создание
                    }
                }
                // Примечание: Роль "Manager" из второго фрагмента здесь не определена.
                // Если она нужна, добавьте ее сюда с соответствующим набором прав.
                // {
                //     "Manager", new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                //     {
                //         Permissions.ViewInventory,
                //         Permissions.ViewTickets
                //         // ... другие права для Manager ...
                //     }
                // }
            };

        public PermissionService(ILogger<PermissionService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            // Изменение: Добавлен контекст в сообщение лога.
            _logger.LogInformation("PermissionService initialized with predefined permissions for {RoleCount} roles.", _rolePermissions.Count);
        }

        // Используем сигнатуру метода из второго фрагмента: (string permissionName, string? roleName)
        // ВАЖНО: Убедитесь, что интерфейс IPermissionService также обновлен!
        public bool HasPermission(string permissionName, string? roleName)
        {
            // Проверка входных данных (используем новые имена параметров)
            if (string.IsNullOrWhiteSpace(roleName))
            {
                _logger.LogWarning("HasPermission check failed: RoleName is empty or null. Permission requested: '{PermissionName}'", permissionName ?? "null");
                return false; // Нет роли - нет прав
            }
            if (string.IsNullOrWhiteSpace(permissionName))
            {
                _logger.LogWarning("HasPermission check failed: PermissionName is empty or null for Role: '{RoleName}'", roleName);
                return false; // Не указано, какое право проверять
            }


            // Администратор все еще имеет все права (особый случай)
            if (roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Permission check for Role '{RoleName}' and Permission '{PermissionName}': Granted (Admin override).", roleName, permissionName);
                return true;
            }

            // Ищем права для указанной роли в словаре (логика из первой версии)
            if (_rolePermissions.TryGetValue(roleName, out var permissions))
            {
                // Проверяем, содержит ли набор прав для этой роли искомое разрешение
                bool granted = permissions.Contains(permissionName); // Используем permissionName
                _logger.LogDebug("Permission check for Role '{RoleName}' and Permission '{PermissionName}': {Result}", roleName, permissionName, granted ? "Granted" : "Denied");
                return granted;
            }
            else
            {
                // Роль не найдена в нашей карте прав
                _logger.LogWarning("Permission check failed: Role '{RoleName}' not found in permission map. Permission '{PermissionName}' denied.", roleName, permissionName);
                return false;
            }
        }
        // ... другие методы, если они есть ...
    }
}