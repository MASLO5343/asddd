// fefe-main/WpfApp1/TestData.cs
using BCrypt.Net; // <-- Убедись, что этот using есть
using System;
using System.Linq;
using WpfApp1.Models; // Для доступа к User, Role и AppDbContext
// using Microsoft.Extensions.Logging; // Раскомментируй, если будешь использовать логгер

namespace WpfApp1 // Убедись, что пространство имен правильное
{
    public class TestData
    {
        private readonly AppDbContext _dbContext;
        // private readonly ILogger<TestData> _logger; // Поле для логгера

        // Конструктор теперь не требует PasswordHasher
        public TestData(AppDbContext dbContext /*, ILogger<TestData> logger = null */)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            // _logger = logger; // Сохраняем логгер, если он передан
        }

        public void CreateTestUser()
        {
            Role adminRole = null;
            try
            {
                // --- Создание или получение роли Admin ---
                // ИЗМЕНЕНИЕ: Исправлено Name на RoleName
                adminRole = _dbContext.Roles.FirstOrDefault(r => r.RoleName == "Admin");
                if (adminRole == null)
                {
                    // _logger?.LogInformation("Admin role not found, creating one.");
                    // ИЗМЕНЕНИЕ: Исправлено Name на RoleName
                    adminRole = new Role { RoleName = "Admin", Description = "Administrator Role" };
                    _dbContext.Roles.Add(adminRole);
                    _dbContext.SaveChanges(); // Сохраняем роль, чтобы получить ее RoleId
                    // ИЗМЕНЕНИЕ: Исправлено Id на RoleId
                    // _logger?.LogInformation("Admin role created with ID {RoleId}.", adminRole.RoleId);
                }
            }
            catch (Exception ex)
            {
                // _logger?.LogError(ex, "Failed to create or find Admin role.");
                Console.WriteLine($"Failed to create or find Admin role: {ex.Message}");
                // Если роль не удалось создать/найти, дальнейшее создание пользователя с этой ролью бессмысленно
                // Можно либо выбросить исключение, либо присвоить null/стандартную роль, если она есть
                throw new InvalidOperationException("Could not ensure Admin role exists.", ex); // Выбрасываем исключение
            }


            // --- Создание пользователя Admin ---
            string adminUsername = "admin";
            // Используем try-catch и для поиска пользователя
            try
            {
                var existingUser = _dbContext.Users.SingleOrDefault(u => u.Username == adminUsername);

                if (existingUser != null)
                {
                    // _logger?.LogInformation("Test user '{Username}' already exists.", adminUsername);
                    // Опциональный код обновления хеша существующего пользователя удален для простоты
                    // Если нужно обновить существующего - лучше сделать это вручную или отдельным методом
                    return; // Пользователь уже есть, выходим
                }
            }
            catch (Exception ex)
            {
                // _logger?.LogError(ex, "Failed to check for existing user '{Username}'.", adminUsername);
                Console.WriteLine($"Failed to check for existing user '{adminUsername}': {ex.Message}");
                throw; // Перевыбрасываем ошибку, если не удалось проверить пользователя
            }


            // Если дошли сюда, пользователя нет, создаем нового
            // _logger?.LogInformation("Creating test user '{Username}'...", adminUsername);
            var user = new User
            {
                Username = adminUsername,
                FullName = "Administrator",
                Email = "admin@example.com",
                IsActive = true,
                CreatedAt = DateTime.UtcNow, // Используем UTC
                // ИЗМЕНЕНИЕ: Исправлено Id на RoleId
                RoleId = adminRole.RoleId    // Присваиваем ID роли Admin
            };

            // Хешируем пароль с помощью BCrypt.Net
            string plainPassword = "1234"; // Убедись, что пароль совпадает с тем, что ты вводишь
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);

            // Сохраняем пользователя в базе данных
            try
            {
                _dbContext.Users.Add(user);
                _dbContext.SaveChanges(); // Сохраняем нового пользователя
                // _logger?.LogInformation("Test user '{Username}' created successfully.", adminUsername);
            }
            catch (Exception ex)
            {
                // _logger?.LogError(ex, "Failed to save test user '{Username}'.", adminUsername);
                Console.WriteLine($"Failed to save test user '{adminUsername}': {ex.Message}");
                // Перевыбрасываем исключение, чтобы оно было поймано в конструкторе LoginWindow
                throw;
            }
        }
    }
}