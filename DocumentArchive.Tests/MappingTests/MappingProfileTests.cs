using AutoMapper;
using DocumentArchive.Core.DTOs.ArchiveLog;
using DocumentArchive.Core.DTOs.Auth;
using DocumentArchive.Core.DTOs.Category;
using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.DTOs.DocumentVersion;
using DocumentArchive.Core.DTOs.Permission;
using DocumentArchive.Core.DTOs.Role;
using DocumentArchive.Core.DTOs.User;
using DocumentArchive.Core.Models;
using DocumentArchive.Services.Mapping;
using FluentAssertions;

namespace DocumentArchive.Tests.MappingTests;

public class MappingProfileTests
{
    private readonly IMapper _mapper;

    public MappingProfileTests()
    {
        var configuration = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = configuration.CreateMapper();
    }

    [Fact]
    public void MappingProfile_Configuration_IsValid()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        config.AssertConfigurationIsValid();
    }

    #region Document Mappings

    [Fact]
    public void CreateDocumentDto_To_Document_Mapping_ShouldMapCorrectly()
    {
        var dto = new CreateDocumentDto
        {
            Title = "Test Doc",
            Description = "Description",
            FileName = "test.pdf",
            CategoryId = Guid.NewGuid(),
            UserId = Guid.NewGuid()
        };

        var document = _mapper.Map<Document>(dto);

        document.Title.Should().Be(dto.Title);
        document.Description.Should().Be(dto.Description);
        document.FileName.Should().Be(dto.FileName);
        document.CategoryId.Should().Be(dto.CategoryId);
        document.UserId.Should().Be(dto.UserId);
    }

    [Fact]
    public void UpdateDocumentDto_To_Document_Mapping_ShouldMapCorrectly()
    {
        var dto = new UpdateDocumentDto
        {
            Title = "Updated",
            Description = "New desc",
            FileName = "new.pdf",
            CategoryId = Guid.NewGuid()
        };

        var document = _mapper.Map<Document>(dto);

        document.Title.Should().Be(dto.Title);
        document.Description.Should().Be(dto.Description);
        document.FileName.Should().Be(dto.FileName);
        document.CategoryId.Should().Be(dto.CategoryId);
    }

    [Fact]
    public void UpdateBulkDocumentDto_To_Document_Mapping_ShouldMapCorrectly()
    {
        var dto = new UpdateBulkDocumentDto
        {
            Id = Guid.NewGuid(),
            Title = "Bulk Updated",
            Description = "Bulk desc",
            FileName = "bulk.pdf",
            CategoryId = Guid.NewGuid()
        };

        var document = _mapper.Map<Document>(dto);

        document.Id.Should().Be(dto.Id);
        document.Title.Should().Be(dto.Title);
        document.Description.Should().Be(dto.Description);
        document.FileName.Should().Be(dto.FileName);
        document.CategoryId.Should().Be(dto.CategoryId);
    }

    [Fact]
    public void Document_To_DocumentResponseDto_Mapping_ShouldMapCorrectly()
    {
        var category = new Category { Name = "Finance" };
        var user = new User { Username = "john" };
        var document = new Document
        {
            Id = Guid.NewGuid(),
            Title = "Report",
            Description = "Annual report",
            FileName = "report.pdf",
            UploadDate = DateTime.UtcNow,
            Category = category,
            User = user
        };

        var dto = _mapper.Map<DocumentResponseDto>(document);

        dto.Id.Should().Be(document.Id);
        dto.Title.Should().Be(document.Title);
        dto.Description.Should().Be(document.Description);
        dto.FileName.Should().Be(document.FileName);
        dto.UploadDate.Should().Be(document.UploadDate);
        dto.CategoryName.Should().Be("Finance");
        dto.UserName.Should().Be("john");
    }

    [Fact]
    public void Document_To_DocumentListItemDto_Mapping_ShouldMapCorrectly()
    {
        var category = new Category { Name = "HR" };
        var document = new Document
        {
            Id = Guid.NewGuid(),
            Title = "Doc",
            UploadDate = DateTime.UtcNow,
            Category = category
        };

        var dto = _mapper.Map<DocumentListItemDto>(document);

        dto.Id.Should().Be(document.Id);
        dto.Title.Should().Be(document.Title);
        dto.CategoryName.Should().Be("HR");
        dto.UploadDate.Should().Be(document.UploadDate);
    }

    #endregion

    #region User Mappings

    [Fact]
    public void CreateUserDto_To_User_Mapping_ShouldMapCorrectly()
    {
        var dto = new CreateUserDto
        {
            Username = "testuser",
            Email = "test@example.com"
        };

        var user = _mapper.Map<User>(dto);

        user.Username.Should().Be(dto.Username);
        user.Email.Should().Be(dto.Email);
        // Поля, которые должны игнорироваться
        user.PasswordHash.Should().BeNull();
        user.FirstName.Should().BeNull();
        user.LastName.Should().BeNull();
        user.DateOfBirth.Should().BeNull();
        user.PhoneNumber.Should().BeNull();
    }

    [Fact]
    public void UpdateUserDto_To_User_Mapping_ShouldMapCorrectly()
    {
        var dto = new UpdateUserDto
        {
            Username = "updated",
            Email = "updated@test.com"
        };

        var user = _mapper.Map<User>(dto);

        user.Username.Should().Be(dto.Username);
        user.Email.Should().Be(dto.Email);
    }

    [Fact]
    public void User_To_UserResponseDto_Mapping_ShouldMapCorrectly()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "john",
            Email = "john@test.com",
            CreatedAt = DateTime.UtcNow,
            FirstName = "John",
            LastName = "Doe"
        };

        var dto = _mapper.Map<UserResponseDto>(user);

        dto.Id.Should().Be(user.Id);
        dto.Username.Should().Be(user.Username);
        dto.Email.Should().Be(user.Email);
        dto.CreatedAt.Should().Be(user.CreatedAt);
    }

    [Fact]
    public void User_To_UserListItemDto_Mapping_ShouldMapCorrectly()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "jane",
            Email = "jane@test.com",
            CreatedAt = DateTime.UtcNow
        };

        var dto = _mapper.Map<UserListItemDto>(user);

        dto.Id.Should().Be(user.Id);
        dto.Username.Should().Be(user.Username);
        dto.Email.Should().Be(user.Email);
    }

    [Fact]
    public void RegisterDto_To_User_Mapping_ShouldMapCorrectly()
    {
        var dto = new RegisterDto
        {
            Username = "newuser",
            Email = "new@test.com",
            FirstName = "New",
            LastName = "User",
            DateOfBirth = new DateTime(1990, 1, 1),
            PhoneNumber = "+123456789"
        };

        var user = _mapper.Map<User>(dto);

        user.Username.Should().Be(dto.Username);
        user.Email.Should().Be(dto.Email);
        user.FirstName.Should().Be(dto.FirstName);
        user.LastName.Should().Be(dto.LastName);
        user.DateOfBirth.Should().Be(dto.DateOfBirth);
        user.PhoneNumber.Should().Be(dto.PhoneNumber);
        // Поля, которые игнорируются при маппинге, остаются со значениями по умолчанию
        user.PasswordHash.Should().BeNull();
        user.LastLoginAt.Should().BeNull();
        user.IsEmailConfirmed.Should().BeFalse(); // по умолчанию false
        user.IsActive.Should().BeTrue(); // по умолчанию true в модели User
        user.IsDeleted.Should().BeFalse();
    }

    #endregion

    #region Category Mappings

    [Fact]
    public void CreateCategoryDto_To_Category_Mapping_ShouldMapCorrectly()
    {
        var dto = new CreateCategoryDto
        {
            Name = "IT",
            Description = "Information Technology"
        };

        var category = _mapper.Map<Category>(dto);

        category.Name.Should().Be(dto.Name);
        category.Description.Should().Be(dto.Description);
    }

    [Fact]
    public void UpdateCategoryDto_To_Category_Mapping_ShouldMapCorrectly()
    {
        var dto = new UpdateCategoryDto
        {
            Name = "Updated",
            Description = "New description"
        };

        var category = _mapper.Map<Category>(dto);

        category.Name.Should().Be(dto.Name);
        category.Description.Should().Be(dto.Description);
    }

    [Fact]
    public void Category_To_CategoryResponseDto_Mapping_ShouldMapCorrectly()
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Finance",
            Description = "Financial docs",
            CreatedAt = DateTime.UtcNow
        };

        var dto = _mapper.Map<CategoryResponseDto>(category);

        dto.Id.Should().Be(category.Id);
        dto.Name.Should().Be(category.Name);
        dto.Description.Should().Be(category.Description);
        dto.CreatedAt.Should().Be(category.CreatedAt);
    }

    [Fact]
    public void Category_To_CategoryListItemDto_Mapping_ShouldMapCorrectly()
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "HR",
            CreatedAt = DateTime.UtcNow
        };

        var dto = _mapper.Map<CategoryListItemDto>(category);

        dto.Id.Should().Be(category.Id);
        dto.Name.Should().Be(category.Name);
    }

    #endregion

    #region ArchiveLog Mappings

    [Fact]
    public void CreateArchiveLogDto_To_ArchiveLog_Mapping_ShouldMapCorrectly()
    {
        var dto = new CreateArchiveLogDto
        {
            Action = "Created",
            ActionType = ActionType.Created,
            IsCritical = true,
            UserId = Guid.NewGuid(),
            DocumentId = Guid.NewGuid()
        };

        var log = _mapper.Map<ArchiveLog>(dto);

        log.Action.Should().Be(dto.Action);
        log.ActionType.Should().Be(dto.ActionType);
        log.IsCritical.Should().Be(dto.IsCritical);
        log.UserId.Should().Be(dto.UserId);
        log.DocumentId.Should().Be(dto.DocumentId);
    }

    [Fact]
    public void ArchiveLog_To_ArchiveLogResponseDto_Mapping_ShouldMapCorrectly()
    {
        var user = new User { Username = "john" };
        var document = new Document { Title = "Report" };
        var log = new ArchiveLog
        {
            Id = Guid.NewGuid(),
            Action = "Created",
            ActionType = ActionType.Created,
            IsCritical = false,
            Timestamp = DateTime.UtcNow,
            User = user,
            Document = document
        };

        var dto = _mapper.Map<ArchiveLogResponseDto>(log);

        dto.Id.Should().Be(log.Id);
        dto.Action.Should().Be(log.Action);
        dto.ActionType.Should().Be(log.ActionType);
        dto.IsCritical.Should().Be(log.IsCritical);
        dto.Timestamp.Should().Be(log.Timestamp);
        dto.UserName.Should().Be("john");
        dto.DocumentTitle.Should().Be("Report");
    }

    [Fact]
    public void ArchiveLog_To_ArchiveLogListItemDto_Mapping_ShouldMapCorrectly()
    {
        var user = new User { Username = "alice" };
        var log = new ArchiveLog
        {
            Id = Guid.NewGuid(),
            Action = "Deleted",
            ActionType = ActionType.Deleted,
            IsCritical = true,
            Timestamp = DateTime.UtcNow,
            User = user
        };

        var dto = _mapper.Map<ArchiveLogListItemDto>(log);

        dto.Id.Should().Be(log.Id);
        dto.Action.Should().Be(log.Action);
        dto.ActionType.Should().Be(log.ActionType);
        dto.IsCritical.Should().Be(log.IsCritical);
        dto.Timestamp.Should().Be(log.Timestamp);
        dto.UserName.Should().Be("alice");
    }

    #endregion

    #region Role Mappings

    [Fact]
    public void CreateRoleDto_To_Role_Mapping_ShouldMapCorrectly()
    {
        var dto = new CreateRoleDto
        {
            Name = "Admin",
            Description = "Administrator role"
        };

        var role = _mapper.Map<Role>(dto);

        role.Name.Should().Be(dto.Name);
        role.Description.Should().Be(dto.Description);
    }

    [Fact]
    public void UpdateRoleDto_To_Role_Mapping_ShouldMapCorrectly()
    {
        var dto = new UpdateRoleDto
        {
            Name = "Moderator",
            Description = "Moderator role"
        };

        var role = _mapper.Map<Role>(dto);

        role.Name.Should().Be(dto.Name);
        role.Description.Should().Be(dto.Description);
    }

    [Fact]
    public void Role_To_RoleResponseDto_Mapping_ShouldMapCorrectly()
    {
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = "User",
            Description = "Regular user",
            CreatedAt = DateTime.UtcNow
        };

        var dto = _mapper.Map<RoleResponseDto>(role);

        dto.Id.Should().Be(role.Id);
        dto.Name.Should().Be(role.Name);
        dto.Description.Should().Be(role.Description);
        dto.CreatedAt.Should().Be(role.CreatedAt);
    }

    [Fact]
    public void Role_To_RoleListItemDto_Mapping_ShouldMapCorrectly()
    {
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = "Manager",
            CreatedAt = DateTime.UtcNow
        };

        var dto = _mapper.Map<RoleListItemDto>(role);

        dto.Id.Should().Be(role.Id);
        dto.Name.Should().Be(role.Name);
    }

    #endregion

    #region Permission Mappings

    [Fact]
    public void CreatePermissionDto_To_Permission_Mapping_ShouldMapCorrectly()
    {
        var dto = new CreatePermissionDto
        {
            Name = "Edit",
            Description = "Edit documents",
            Category = "Documents"
        };

        var permission = _mapper.Map<Permission>(dto);

        permission.Name.Should().Be(dto.Name);
        permission.Description.Should().Be(dto.Description);
        permission.Category.Should().Be(dto.Category);
    }

    [Fact]
    public void UpdatePermissionDto_To_Permission_Mapping_ShouldMapCorrectly()
    {
        var dto = new UpdatePermissionDto
        {
            Name = "Delete",
            Description = "Delete documents",
            Category = "Admin"
        };

        var permission = _mapper.Map<Permission>(dto);

        permission.Name.Should().Be(dto.Name);
        permission.Description.Should().Be(dto.Description);
        permission.Category.Should().Be(dto.Category);
    }

    [Fact]
    public void Permission_To_PermissionResponseDto_Mapping_ShouldMapCorrectly()
    {
        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            Name = "View",
            Description = "View documents",
            Category = "Documents",
            CreatedAt = DateTime.UtcNow
        };

        var dto = _mapper.Map<PermissionResponseDto>(permission);

        dto.Id.Should().Be(permission.Id);
        dto.Name.Should().Be(permission.Name);
        dto.Description.Should().Be(permission.Description);
        dto.Category.Should().Be(permission.Category);
        dto.CreatedAt.Should().Be(permission.CreatedAt);
    }

    [Fact]
    public void Permission_To_PermissionListItemDto_Mapping_ShouldMapCorrectly()
    {
        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            Name = "Upload",
            Category = "Documents",
            CreatedAt = DateTime.UtcNow
        };

        var dto = _mapper.Map<PermissionListItemDto>(permission);

        dto.Id.Should().Be(permission.Id);
        dto.Name.Should().Be(permission.Name);
        dto.Category.Should().Be(permission.Category);
    }

    #endregion

    #region DocumentVersion Mappings

    [Fact]
    public void CreateDocumentVersionDto_To_DocumentVersion_Mapping_ShouldMapCorrectly()
    {
        var dto = new CreateDocumentVersionDto
        {
            DocumentId = Guid.NewGuid(),
            VersionNumber = 2,
            FileName = "v2.pdf",
            FileSize = 2048,
            Comment = "Second version"
        };

        var version = _mapper.Map<DocumentVersion>(dto);

        version.DocumentId.Should().Be(dto.DocumentId);
        version.VersionNumber.Should().Be(dto.VersionNumber);
        version.FileName.Should().Be(dto.FileName);
        version.FileSize.Should().Be(dto.FileSize);
        version.Comment.Should().Be(dto.Comment);
    }

    [Fact]
    public void UpdateDocumentVersionDto_To_DocumentVersion_Mapping_ShouldMapCorrectly()
    {
        var dto = new UpdateDocumentVersionDto
        {
            Comment = "Updated comment"
        };

        var version = _mapper.Map<DocumentVersion>(dto);

        version.Comment.Should().Be(dto.Comment);
        // Другие поля игнорируются
    }

    [Fact]
    public void DocumentVersion_To_DocumentVersionResponseDto_Mapping_ShouldMapCorrectly()
    {
        var doc = new Document { Title = "Doc" };
        var version = new DocumentVersion
        {
            Id = Guid.NewGuid(),
            DocumentId = doc.Id,
            VersionNumber = 1,
            FileName = "v1.pdf",
            FileSize = 1024,
            Comment = "Initial",
            UploadedAt = DateTime.UtcNow,
            UploadedBy = Guid.NewGuid(),
            Document = doc
        };

        var dto = _mapper.Map<DocumentVersionResponseDto>(version);

        dto.Id.Should().Be(version.Id);
        dto.DocumentId.Should().Be(version.DocumentId);
        dto.VersionNumber.Should().Be(version.VersionNumber);
        dto.FileName.Should().Be(version.FileName);
        dto.FileSize.Should().Be(version.FileSize);
        dto.Comment.Should().Be(version.Comment);
        dto.UploadedAt.Should().Be(version.UploadedAt);
        dto.UploadedBy.Should().Be(version.UploadedBy);
    }

    [Fact]
    public void DocumentVersion_To_DocumentVersionListItemDto_Mapping_ShouldMapCorrectly()
    {
        var version = new DocumentVersion
        {
            Id = Guid.NewGuid(),
            DocumentId = Guid.NewGuid(),
            VersionNumber = 3,
            FileName = "v3.pdf",
            UploadedAt = DateTime.UtcNow,
            UploadedBy = Guid.NewGuid()
        };

        var dto = _mapper.Map<DocumentVersionListItemDto>(version);

        dto.Id.Should().Be(version.Id);
        dto.VersionNumber.Should().Be(version.VersionNumber);
        dto.FileName.Should().Be(version.FileName);
        dto.UploadedAt.Should().Be(version.UploadedAt);
    }

    #endregion
}