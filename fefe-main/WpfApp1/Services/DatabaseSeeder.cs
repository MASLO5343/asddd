// WpfApp1/Services/DatabaseSeeder.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using WpfApp1.Interfaces;
using WpfApp1.Models; // Required for AppDbContext, Role, User

namespace WpfApp1.Services
{
    public class DatabaseSeeder : IDataSeeder
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DatabaseSeeder> _logger;

        public DatabaseSeeder(IServiceProvider serviceProvider, ILogger<DatabaseSeeder> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            _logger.LogInformation("Starting database seeding process.");
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var roleService = scope.ServiceProvider.GetRequiredService<IRoleService>();
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                await context.Database.EnsureCreatedAsync();

                _logger.LogInformation("Creating Admin role if it doesn't exist.");
                Role? adminRole = await roleService.GetRoleByNameAsync("Admin");
                if (adminRole == null)
                {
                    Role newAdminRole = new Role { RoleName = "Admin" };
                    adminRole = await roleService.CreateRoleAsync(newAdminRole); // CreateRoleAsync returns the created Role or null
                    if (adminRole == null)
                    {
                        _logger.LogError("Failed to create Admin role."); // Generic error, specific error might be logged in RoleService
                        return;
                    }
                    _logger.LogInformation("Admin role created successfully with ID: {RoleId}.", adminRole.RoleId);
                }
                else
                {
                    _logger.LogInformation("Admin role already exists with ID: {RoleId}.", adminRole.RoleId);
                }

                _logger.LogInformation("Creating User role if it doesn't exist.");
                Role? userRole = await roleService.GetRoleByNameAsync("User");
                if (userRole == null)
                {
                    Role newUserRole = new Role { RoleName = "User" };
                    userRole = await roleService.CreateRoleAsync(newUserRole); // CreateRoleAsync returns the created Role or null
                    if (userRole == null)
                    {
                        _logger.LogError("Failed to create User role."); // Generic error
                        return;
                    }
                    _logger.LogInformation("User role created successfully with ID: {RoleId}.", userRole.RoleId);
                }
                else
                {
                    _logger.LogInformation("User role already exists with ID: {RoleId}.", userRole.RoleId);
                }

                // Ensure adminRole is not null before proceeding (it should not be if logic above is correct and no return occurred)
                if (adminRole == null)
                {
                    _logger.LogError("Admin role is null, cannot create admin user. Seeding process aborted.");
                    return;
                }

                _logger.LogInformation("Checking for default Admin user.");
                User? adminUser = await userService.GetUserByUsernameAsync("admin");
                if (adminUser == null)
                {
                    _logger.LogInformation("Default Admin user not found, creating one.");
                    var newAdminUser = new User
                    {
                        Username = "admin",
                        FullName = "Default Admin", // Corrected: Use FullName
                        IsActive = true,
                        RoleId = adminRole.RoleId
                    };

                    // ЗАМЕНИТЕ НА БОЛЕЕ СЛОЖНЫЙ ПАРОЛЬ ПО УМОЛЧАНИЮ
                    User? createdAdminUser = await userService.CreateUserAsync(newAdminUser, "Password123!"); // CreateUserAsync returns User or null
                    if (createdAdminUser == null)
                    {
                        _logger.LogError("Failed to create default Admin user."); // Generic error
                        return;
                    }
                    _logger.LogInformation("Default Admin user created successfully with ID: {UserId}", createdAdminUser.UserId);
                }
                else
                {
                    _logger.LogInformation("Default Admin user already exists with ID: {UserId}", adminUser.UserId);
                }
                _logger.LogInformation("Database seeding process completed.");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "An error occurred during database seeding.");
            }
        }
    }
}