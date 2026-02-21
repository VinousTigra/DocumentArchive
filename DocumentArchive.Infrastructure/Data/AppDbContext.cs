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
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<SecurityAuditLog> SecurityAuditLogs => Set<SecurityAuditLog>();
    public DbSet<UserClaim> UserClaims => Set<UserClaim>();

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

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(us => us.Id);
            entity.Property(us => us.RefreshTokenHash)
                .IsRequired()
                .HasMaxLength(500);
            entity.Property(us => us.DeviceInfo).HasMaxLength(500);
            entity.Property(us => us.IpAddress).HasMaxLength(50);
            entity.Property(us => us.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(us => us.User)
                .WithMany(u => u.Sessions) // если добавите навигацию в User
                .HasForeignKey(us => us.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(us => us.RefreshTokenHash);
            entity.HasIndex(us => us.ExpiresAt);
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

            entity.Property(ur => ur.AssignedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
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

        // ===== Seed Permissions =====
        var viewDocsPermId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var uploadDocsPermId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var editOwnDocsPermId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var editAnyDocsPermId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        var deleteOwnDocsPermId = Guid.Parse("88888888-8888-8888-8888-888888888888");
        var deleteAnyDocsPermId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var manageUsersPermId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var viewAuditLogsPermId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var manageRolesPermId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        modelBuilder.Entity<Permission>().HasData(
            new Permission
            {
                Id = viewDocsPermId, Name = "ViewDocuments", Description = "Просмотр документов",
                Category = "Documents", CreatedAt = seedDate
            },
            new Permission
            {
                Id = uploadDocsPermId, Name = "UploadDocuments", Description = "Загрузка документов",
                Category = "Documents", CreatedAt = seedDate
            },
            new Permission
            {
                Id = editOwnDocsPermId, Name = "EditOwnDocuments", Description = "Редактирование своих документов",
                Category = "Documents", CreatedAt = seedDate
            },
            new Permission
            {
                Id = editAnyDocsPermId, Name = "EditAnyDocument", Description = "Редактирование любых документов",
                Category = "Documents", CreatedAt = seedDate
            },
            new Permission
            {
                Id = deleteOwnDocsPermId, Name = "DeleteOwnDocuments", Description = "Удаление своих документов",
                Category = "Documents", CreatedAt = seedDate
            },
            new Permission
            {
                Id = deleteAnyDocsPermId, Name = "DeleteAnyDocument", Description = "Удаление любых документов",
                Category = "Documents", CreatedAt = seedDate
            },
            new Permission
            {
                Id = manageUsersPermId, Name = "ManageUsers", Description = "Управление пользователями",
                Category = "Administration", CreatedAt = seedDate
            },
            new Permission
            {
                Id = viewAuditLogsPermId, Name = "ViewAuditLogs", Description = "Просмотр логов аудита",
                Category = "Administration", CreatedAt = seedDate
            },
            new Permission
            {
                Id = manageRolesPermId, Name = "ManageRoles", Description = "Управление ролями и правами",
                Category = "Administration", CreatedAt = seedDate
            }
        );

// ===== Seed RolePermissions =====
// Admin получает все права
        modelBuilder.Entity<RolePermission>().HasData(
            new RolePermission { RoleId = adminRoleId, PermissionId = viewDocsPermId },
            new RolePermission { RoleId = adminRoleId, PermissionId = uploadDocsPermId },
            new RolePermission { RoleId = adminRoleId, PermissionId = editOwnDocsPermId },
            new RolePermission { RoleId = adminRoleId, PermissionId = editAnyDocsPermId },
            new RolePermission { RoleId = adminRoleId, PermissionId = deleteOwnDocsPermId },
            new RolePermission { RoleId = adminRoleId, PermissionId = deleteAnyDocsPermId },
            new RolePermission { RoleId = adminRoleId, PermissionId = manageUsersPermId },
            new RolePermission { RoleId = adminRoleId, PermissionId = viewAuditLogsPermId },
            new RolePermission { RoleId = adminRoleId, PermissionId = manageRolesPermId }
        );

// Moderator
        modelBuilder.Entity<RolePermission>().HasData(
            new RolePermission { RoleId = moderatorRoleId, PermissionId = viewDocsPermId },
            new RolePermission { RoleId = moderatorRoleId, PermissionId = uploadDocsPermId },
            new RolePermission { RoleId = moderatorRoleId, PermissionId = editAnyDocsPermId },
            new RolePermission { RoleId = moderatorRoleId, PermissionId = viewAuditLogsPermId }
        );

// User
        modelBuilder.Entity<RolePermission>().HasData(
            new RolePermission { RoleId = userRoleId, PermissionId = viewDocsPermId },
            new RolePermission { RoleId = userRoleId, PermissionId = uploadDocsPermId },
            new RolePermission { RoleId = userRoleId, PermissionId = editOwnDocsPermId },
            new RolePermission { RoleId = userRoleId, PermissionId = deleteOwnDocsPermId }
        );
        // password
        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(t => t.User)
                .WithMany() // можно добавить коллекцию в User, если нужно
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(t => t.ExpiresAt);
        });

        modelBuilder.Entity<SecurityAuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).HasConversion<int>();
            entity.Property(e => e.UserEmail).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.Timestamp);
        });


        modelBuilder.Entity<UserClaim>(entity =>
        {
            entity.HasKey(uc => uc.Id);
            entity.Property(uc => uc.ClaimType).IsRequired().HasMaxLength(100);
            entity.Property(uc => uc.ClaimValue).IsRequired().HasMaxLength(500);
            entity.Property(uc => uc.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(uc => uc.User)
                .WithMany(u => u.UserClaims) 
                .HasForeignKey(uc => uc.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(uc => uc.ClaimType);
        });

        base.OnModelCreating(modelBuilder);
    }
}