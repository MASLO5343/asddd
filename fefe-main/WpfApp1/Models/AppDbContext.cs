// СОГЛАСОВАНО: Начинаем с AppDbContext.cs

using Microsoft.EntityFrameworkCore;
using WpfApp1.Models; // Убедитесь, что все ваши модели находятся в этом пространстве имен

namespace WpfApp1.Models // Или где у вас лежит AppDbContext
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // DbSets для всех сущностей, соответствующих "Правки.docx"
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<Device> Devices { get; set; } = null!;
        public DbSet<DeviceType> DeviceTypes { get; set; } = null!; // Предполагается на основе Device.TypeId
        public DbSet<Software> Software { get; set; } = null!; // Имя таблицы может быть Softwares по соглашению EF
        public DbSet<DeviceSoftware> DeviceSoftwares { get; set; } = null!;
        public DbSet<Ticket> Tickets { get; set; } = null!;
        public DbSet<TicketComment> TicketComments { get; set; } = null!;
        public DbSet<Log> Logs { get; set; } = null!;
        public DbSet<AutomationTask> AutomationTasks { get; set; } = null!; // Если используется
        public DbSet<TicketStatus> TicketStatuses { get; set; } = null!;
        public DbSet<TicketPriority> TicketPriorities { get; set; } = null!;
        public DbSet<DeviceStatus> DeviceStatuses { get; set; } = null!; // DbSet для статусов устройств

        // Другие DbSets, если они есть и соответствуют "Правкам"


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User Configuration (согласно "Правки.docx")
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.Username).IsUnique(); // Индекс для Username (уникальный логин)
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255); // NVARCHAR(MAX) в правках, но 255 для хеша обычно достаточно, если нет - меняем
                entity.Property(e => e.FullName).HasMaxLength(100);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.IsActive).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();

                entity.HasOne(e => e.Role)
                      .WithMany(r => r.Users)
                      .HasForeignKey(e => e.RoleId)
                      .IsRequired(); // Роль обязательна для пользователя
            });

            // Role Configuration (согласно "Правки.docx")
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.RoleId);
                entity.Property(e => e.RoleName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(255);
            });

            // Device Configuration (согласно "Правки.docx" и нашим моделям)
            modelBuilder.Entity<Device>(entity =>
            {
                entity.HasKey(e => e.DeviceId);
                entity.Property(e => e.InventoryNumber).HasMaxLength(50);
                entity.HasIndex(e => e.InventoryNumber); // Индекс для InventoryNumber

                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.IpAddress).HasMaxLength(50); // Было IPAddress в "Правках"
                entity.Property(e => e.MacAddress).HasMaxLength(50); // Было MACAddress в "Правках"
                entity.Property(e => e.Location).HasMaxLength(100);
                entity.Property(e => e.Department).HasMaxLength(100);
                // entity.Property(e => e.Status) - enum, конфигурируется по умолчанию
                entity.Property(e => e.WarrantyUntil); // DateOnly? будет преобразовано в date в SQL Server

                entity.HasOne(d => d.DeviceType) // Используем корректное имя навигационного свойства
                      .WithMany(dt => dt.Devices) // Предполагается, что в DeviceType есть коллекция Devices
                      .HasForeignKey(d => d.DeviceTypeId); // Используем корректное имя внешнего ключа

entity.HasOne(d => d.ResponsibleUser) // Используем существующее свойство
      .WithMany() // Если у User нет прямой коллекции Devices, за которые он ответственен
      .HasForeignKey(d => d.ResponsibleUserId) // Используем существующий FK
      .IsRequired(false);
            });

            // DeviceType Configuration (предполагаемая таблица)
            modelBuilder.Entity<DeviceType>(entity =>
            {
                entity.HasKey(e => e.DeviceTypeId); // Предполагаемое имя PK
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                // Другие поля, если есть
            });

            // Software Configuration (Пункт 10, согласно "Правкам" и модели Software.cs)
            modelBuilder.Entity<Software>(entity =>
            {
                entity.HasKey(e => e.SoftwareId); // Используем SoftwareId как в модели
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Version).HasMaxLength(50);

                // Явная конфигурация для Vendor и Notes согласно решению
                entity.Property(e => e.Vendor).HasMaxLength(100); // Предполагаемая длина для производителя
                entity.Property(e => e.Notes).HasColumnType("nvarchar(max)"); // Для длинных заметок

                entity.Property(e => e.LicenseKey).HasMaxLength(255); // Было в вашей конфигурации
                // Поля Manufacturer и LicenseType удалены из конфигурации, т.к. их нет в модели Software.cs / "Правках"
            });

            // DeviceSoftware Configuration (Пункт 11, согласно "Правкам")
            modelBuilder.Entity<DeviceSoftware>(entity =>
            {
                // Единичный первичный ключ Id
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd(); // Автоинкремент

                // Внешние ключи
                entity.HasOne(ds => ds.Device)
                      .WithMany(d => d.DeviceSoftwares)
                      .HasForeignKey(ds => ds.DeviceId)
                      .IsRequired();

                entity.HasOne(ds => ds.Software)
                      .WithMany(s => s.DeviceSoftwares)
                      .HasForeignKey(ds => ds.SoftwareId)
                      .IsRequired();

                entity.Property(e => e.InstalledAt).IsRequired(); // Ранее InstallDate

                // Поле Notes удалено из модели, конфигурация не нужна
            });

            // Ticket Configuration (Пункт 7 и 8, согласно "Правкам")
            modelBuilder.Entity<Ticket>(entity =>
            {
                entity.HasKey(e => e.TicketId);
                entity.HasIndex(e => e.TicketId); // PK уже индексирован, но "Правки" упоминают индекс по TicketId

                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                // Status и Priority - enums

                // Новое поле Category
                entity.Property(e => e.Category).IsRequired().HasMaxLength(100);

                entity.HasOne(t => t.Requester)
                      .WithMany(u => u.RequestedTickets) // Предполагается коллекция в User
                      .HasForeignKey(t => t.RequesterId)
                      .OnDelete(DeleteBehavior.Restrict) // Чтобы нельзя было удалить пользователя, если у него есть заявки
                      .IsRequired();

                entity.HasOne(t => t.Assignee)
                      .WithMany(u => u.AssignedTickets) // Предполагается коллекция в User
                      .HasForeignKey(t => t.AssigneeId)
                      .OnDelete(DeleteBehavior.Restrict) // Чтобы нельзя было удалить исполнителя, если на нем заявки
                      .IsRequired(false); // Исполнитель может быть не назначен

                entity.HasOne(t => t.Device)
                      .WithMany(d => d.Tickets)
                      .HasForeignKey(t => t.DeviceId)
                      .IsRequired(false); // Устройство может быть не указано
                entity.HasOne(t => t.Status)
                       .WithMany(ts => ts.Tickets)
                       .HasForeignKey(t => t.StatusId)
                        .IsRequired();

                entity.HasOne(t => t.Priority)
                      .WithMany(tp => tp.Tickets)
                      .HasForeignKey(t => t.PriorityId)
                      .IsRequired();

                // Также убедиться, что поля RequesterId и AssigneeId корректно настроены для связи с User
                entity.HasOne(t => t.Requester)
                      .WithMany(u => u.RequestedTickets) // В User.cs должна быть коллекция RequestedTickets
                      .HasForeignKey(t => t.RequesterId)
                      .OnDelete(DeleteBehavior.Restrict) // Пример
                      .IsRequired();

                entity.HasOne(t => t.Assignee)
                      .WithMany(u => u.AssignedTickets) // В User.cs должна быть коллекция AssignedTickets
                      .HasForeignKey(t => t.AssigneeId)
                      .OnDelete(DeleteBehavior.Restrict) // Пример
                      .IsRequired(false);
            });

            // TicketComment Configuration (Пункт 9, согласно "Правкам")
            modelBuilder.Entity<TicketComment>(entity =>
            {
                entity.HasKey(e => e.TicketCommentId);
                entity.Property(e => e.Comment).IsRequired(); // Было CommentText
                entity.Property(e => e.CreatedAt).IsRequired();

                entity.HasOne(tc => tc.Ticket)
                      .WithMany(t => t.TicketComments)
                      .HasForeignKey(tc => tc.TicketId)
                      .IsRequired();

                entity.HasOne(tc => tc.Author)
                      .WithMany(u => u.TicketCommentsAuthored) // Используем TicketCommentsAuthored
                      .HasForeignKey(tc => tc.AuthorId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .IsRequired();
            });

            // Log Configuration (Пункт 1, 2 согласно "Правкам")
            modelBuilder.Entity<Log>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Message).IsRequired();
                entity.Property(e => e.Level).IsRequired().HasMaxLength(128);
                entity.Property(e => e.TimeStamp).IsRequired();
                entity.Property(e => e.Properties).HasColumnType("xml");

                // Новые поля согласно "Правкам"
                entity.Property(e => e.IPAddress).HasMaxLength(50); // Было IPAddress
                entity.HasIndex(e => e.IPAddress); // Индекс для IPAddress

                entity.Property(e => e.TableAffected).HasMaxLength(50); // Было EntityType/EntityId

                // Связь с User через UserId
                entity.HasOne(l => l.User)
                      .WithMany(u => u.LogsRecorded) // Используем LogsRecorded
                      .HasForeignKey(l => l.UserId)
                      .IsRequired(false);

                // Индекс по UserId (или UserName, если он будет часто использоваться для поиска в логах)
                // "Правки" упоминают Username для индекса логов. Если UserId есть, и UserName дублируется в Log, то индекс по UserId
                // или по UserName. Если UserName не дублируется, то эффективнее по UserId.
                // Пока сделаем по UserId, если он заполняется.
                entity.HasIndex(e => e.UserId);
                // Если UserName будет отдельным полем в Log и заполняться, можно и по нему:
                // entity.Property(e => e.UserName).HasMaxLength(128);
                // entity.HasIndex(e => e.UserName);
            });

            // AutomationTask Configuration (предполагаемая, если используется)
            modelBuilder.Entity<AutomationTask>(entity =>
            {
                entity.HasKey(e => e.TaskId);
                // ... другие свойства и связи ...
                entity.HasOne(at => at.TargetDevice)
                      .WithMany(d => d.AutomationTasks)
                      .HasForeignKey(at => at.TargetDeviceId);
            });


            // Заполнение начальными данными (сидинг) - если DatabaseSeeder используется для этого
            // modelBuilder.Seed(); // Пример вызова, если у вас есть метод расширения для сидинга
        }
    }
}

// Вам также нужно будет убедиться, что в модели User есть соответствующие коллекции, если вы их определяете:
// public virtual ICollection<Ticket> RequestedTickets { get; set; } = new HashSet<Ticket>();
// public virtual ICollection<Ticket> AssignedTickets { get; set; } = new HashSet<Ticket>();
// public virtual ICollection<TicketComment> TicketComments { get; set; } = new HashSet<TicketComment>();
// public virtual ICollection<Log> Logs { get; set; } = new HashSet<Log>();