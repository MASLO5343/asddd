using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration; // Добавлено
using System.IO;                       // Добавлено

namespace WpfApp1.Models
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            // --- Чтение конфигурации ---
            // Строим путь к appsettings.json относительно текущего файла
            // Обычно папка Migrations находится внутри проекта WpfApp1
            string basePath = Directory.GetCurrentDirectory();
            // Если запускается из папки проекта (где .csproj), то путь будет верным.
            // Если из другого места, возможно, потребуется настроить путь более гибко.
            // Например, ../../../../WpfApp1 (зависит от структуры)
            // Или использовать переменные окружения для указания пути.

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json") // Имя файла конфигурации
                .Build();

            // --- Получение строки подключения ---
            var builder = new DbContextOptionsBuilder<AppDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Could not find connection string 'DefaultConnection'");
            }

            builder.UseSqlServer(connectionString);

            return new AppDbContext(builder.Options);
        }
    }
}