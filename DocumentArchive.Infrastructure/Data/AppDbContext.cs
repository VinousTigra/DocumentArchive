using DocumentArchive.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentArchive.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<ArchiveLog> ArchiveLogs => Set<ArchiveLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Настройка User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(100);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            // Для PostgreSQL можно использовать "NOW()"
        });

        // Настройка Role
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Name).IsRequired().HasMaxLength(50);
            entity.HasIndex(r => r.Name).IsUnique();
            entity.Property(r => r.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Настройка Permission
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(p => p.Name).IsUnique();
            entity.Property(p => p.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Настройка UserRole (составной ключ)
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(ur => new { ur.UserId, ur.RoleId });

            entity.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade); // При удалении пользователя удаляются его связи с ролями

            entity.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade); // При удалении роли удаляются связи
        });

        // Настройка RolePermission (составной ключ)
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(rp => new { rp.RoleId, rp.PermissionId });

            entity.HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Настройка Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(c => c.Name).IsUnique();
            entity.Property(c => c.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            // UpdatedAt может обновляться вручную, поэтому без дефолта
        });

        // Настройка Document
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Title).IsRequired().HasMaxLength(200);
            entity.Property(d => d.FileName).IsRequired().HasMaxLength(255);
            entity.Property(d => d.UploadDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Связь с User
            entity.HasOne(d => d.User)
                .WithMany(u => u.Documents)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior
                    .SetNull); // При удалении пользователя документы остаются, но UserId становится null

            // Связь с Category
            entity.HasOne(d => d.Category)
                .WithMany(c => c.Documents)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Restrict); // Запрещаем удаление категории, если есть документы
        });

        // Настройка DocumentVersion
        modelBuilder.Entity<DocumentVersion>(entity =>
        {
            entity.HasKey(dv => dv.Id);
            entity.Property(dv => dv.FileName).IsRequired().HasMaxLength(255);
            entity.Property(dv => dv.UploadedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(dv => dv.VersionNumber).IsRequired();

            // Связь с Document
            entity.HasOne(dv => dv.Document)
                .WithMany(d => d.Versions)
                .HasForeignKey(dv => dv.DocumentId)
                .OnDelete(DeleteBehavior.Cascade); // При удалении документа удаляются все его версии

            // Опционально: если хотите связать с пользователем, загрузившим версию
            // entity.HasOne(dv => dv.UploadedByUser)... и т.д.
        });

        // Настройка ArchiveLog
        modelBuilder.Entity<ArchiveLog>(entity =>
        {
            entity.HasKey(al => al.Id);
            entity.Property(al => al.Action).IsRequired().HasMaxLength(50);
            entity.Property(al => al.ActionType).HasConversion<int>(); // enum хранится как int
            entity.Property(al => al.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Связь с User
            entity.HasOne(al => al.User)
                .WithMany(u => u.Logs)
                .HasForeignKey(al => al.UserId)
                .OnDelete(DeleteBehavior.SetNull); // При удалении пользователя логи остаются, но UserId = null

            // Связь с Document
            entity.HasOne(al => al.Document)
                .WithMany(d => d.Logs) // если добавили коллекцию Logs в Document
                .HasForeignKey(al => al.DocumentId)
                .OnDelete(DeleteBehavior.Cascade); // При удалении документа логи удаляются
        });

        // Seed-данные (опционально, для начальных ролей)
        // Можно добавить через HasData, но проще через миграции или отдельный скрипт.

        base.OnModelCreating(modelBuilder);
    }
}