using DocumentArchive.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentArchive.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // DbSet для каждой сущности (таблицы)
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
        // Здесь будем настраивать связи, индексы, ограничения с помощью Fluent API
        base.OnModelCreating(modelBuilder);
    }
}