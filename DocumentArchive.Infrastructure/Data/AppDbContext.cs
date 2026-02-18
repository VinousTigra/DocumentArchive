using DocumentArchive.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentArchive.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // DbSet для всех сущностей
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
        // Настройка сущности User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(u => u.FirstName)
                .HasMaxLength(100);
            entity.Property(u => u.LastName)
                .HasMaxLength(100);
            entity.Property(u => u.PhoneNumber)
                .HasMaxLength(20);
            entity.Property(u => u.PasswordHash)
                .HasMaxLength(500);
            entity.Property(u => u.PasswordSalt)
                .HasMaxLength(500);
            entity.Property(u => u.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(u => u.IsActive)
                .HasDefaultValue(true);
            entity.Property(u => u.IsDeleted)
                .HasDefaultValue(false);

            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.Username).IsUnique();
        });

        // Настройка сущности Role
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Name)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(r => r.Description)
                .HasMaxLength(200);
            entity.Property(r => r.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(r => r.Name).IsUnique();
        });

        // Настройка сущности Permission
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(p => p.Description)
                .HasMaxLength(200);
            entity.Property(p => p.Category)
                .HasMaxLength(50);
            entity.Property(p => p.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(p => p.Name).IsUnique();
        });

        // Настройка сущности UserRole (связь many-to-many между User и Role)
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(ur => new { ur.UserId, ur.RoleId });

            entity.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Настройка сущности RolePermission (связь many-to-many между Role и Permission)
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

        // Настройка сущности Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(c => c.Description)
                .HasMaxLength(500);
            entity.Property(c => c.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(c => c.Name).IsUnique();
        });

        // Настройка сущности Document
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Title)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(d => d.Description)
                .HasMaxLength(1000);
            entity.Property(d => d.FileName)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(d => d.UploadDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.User)
                .WithMany(u => u.Documents)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.Category)
                .WithMany(c => c.Documents)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Настройка сущности DocumentVersion
        modelBuilder.Entity<DocumentVersion>(entity =>
        {
            entity.HasKey(dv => dv.Id);
            entity.Property(dv => dv.FileName)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(dv => dv.VersionNumber)
                .IsRequired();
            entity.Property(dv => dv.Comment)
                .HasMaxLength(500);
            entity.Property(dv => dv.UploadedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(dv => dv.Document)
                .WithMany(d => d.Versions)
                .HasForeignKey(dv => dv.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Настройка сущности ArchiveLog
        modelBuilder.Entity<ArchiveLog>(entity =>
        {
            entity.HasKey(al => al.Id);
            entity.Property(al => al.Action)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(al => al.ActionType)
                .HasConversion<int>();
            entity.Property(al => al.IsCritical)
                .IsRequired();
            entity.Property(al => al.Timestamp)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(al => al.User)
                .WithMany(u => u.Logs)
                .HasForeignKey(al => al.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(al => al.Document)
                .WithMany(d => d.Logs)
                .HasForeignKey(al => al.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed-данные для ролей (используем статические GUID и фиксированную дату)
        var adminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var userRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var moderatorRoleId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Role>().HasData(
            new Role
            {
                Id = adminRoleId,
                Name = "Admin",
                Description = "Administrator with full access",
                CreatedAt = seedDate
            },
            new Role
            {
                Id = userRoleId,
                Name = "User",
                Description = "Regular user",
                CreatedAt = seedDate
            },
            new Role
            {
                Id = moderatorRoleId,
                Name = "Moderator",
                Description = "Moderator with limited administrative rights",
                CreatedAt = seedDate
            }
        );

        base.OnModelCreating(modelBuilder);
    }
}