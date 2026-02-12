using AutoMapper;
using DocumentArchive.Core.DTOs.ArchiveLog;
using DocumentArchive.Core.DTOs.Category;
using DocumentArchive.Core.DTOs.Document;
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
        // Создаём конфигурацию ОДИН раз для всех тестов маппинга
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });
        _mapper = configuration.CreateMapper();
    }

    [Fact]
    public void MappingProfile_Configuration_IsValid()
    {
        // Создаём новую конфигурацию ТОЛЬКО для проверки валидности
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });
        
        // Этот метод проверит, что все маппинги корректны и нет дубликатов
        config.AssertConfigurationIsValid();
    }

    [Fact]
    public void CreateDocumentDto_To_Document_Mapping()
    {
        // Arrange
        var dto = new CreateDocumentDto
        {
            Title = "Test Doc",
            Description = "Description",
            FileName = "test.pdf",
            CategoryId = Guid.NewGuid(),
            UserId = Guid.NewGuid()
        };

        // Act
        var document = _mapper.Map<Document>(dto);

        // Assert
        document.Title.Should().Be(dto.Title);
        document.Description.Should().Be(dto.Description);
        document.FileName.Should().Be(dto.FileName);
        document.CategoryId.Should().Be(dto.CategoryId);
        document.UserId.Should().Be(dto.UserId);
    }

    [Fact]
    public void UpdateDocumentDto_To_Document_Mapping()
    {
        // Arrange
        var dto = new UpdateDocumentDto
        {
            Title = "Updated",
            Description = "New desc",
            FileName = "new.pdf",
            CategoryId = Guid.NewGuid()
        };

        // Act
        var document = _mapper.Map<Document>(dto);

        // Assert
        document.Title.Should().Be(dto.Title);
        document.Description.Should().Be(dto.Description);
        document.FileName.Should().Be(dto.FileName);
        document.CategoryId.Should().Be(dto.CategoryId);
    }

    [Fact]
    public void UpdateBulkDocumentDto_To_Document_Mapping()
    {
        // Arrange
        var dto = new UpdateBulkDocumentDto
        {
            Id = Guid.NewGuid(),
            Title = "Bulk Updated",
            Description = "Bulk desc",
            FileName = "bulk.pdf",
            CategoryId = Guid.NewGuid()
        };

        // Act
        var document = _mapper.Map<Document>(dto);

        // Assert
        document.Id.Should().Be(dto.Id);
        document.Title.Should().Be(dto.Title);
        document.Description.Should().Be(dto.Description);
        document.FileName.Should().Be(dto.FileName);
        document.CategoryId.Should().Be(dto.CategoryId);
    }

    [Fact]
    public void Document_To_DocumentResponseDto_Mapping()
    {
        // Arrange
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

        // Act
        var dto = _mapper.Map<DocumentResponseDto>(document);

        // Assert
        dto.Id.Should().Be(document.Id);
        dto.Title.Should().Be(document.Title);
        dto.Description.Should().Be(document.Description);
        dto.FileName.Should().Be(document.FileName);
        dto.UploadDate.Should().Be(document.UploadDate);
        dto.CategoryName.Should().Be("Finance");
        dto.UserName.Should().Be("john");
    }

    [Fact]
    public void Document_To_DocumentListItemDto_Mapping()
    {
        // Arrange
        var category = new Category { Name = "HR" };
        var document = new Document
        {
            Id = Guid.NewGuid(),
            Title = "Doc",
            UploadDate = DateTime.UtcNow,
            Category = category
        };

        // Act
        var dto = _mapper.Map<DocumentListItemDto>(document);

        // Assert
        dto.Id.Should().Be(document.Id);
        dto.Title.Should().Be(document.Title);
        dto.CategoryName.Should().Be("HR");
        dto.UploadDate.Should().Be(document.UploadDate);
    }

    [Fact]
    public void CreateUserDto_To_User_Mapping()
    {
        // Arrange
        var dto = new CreateUserDto
        {
            Username = "testuser",
            Email = "test@example.com"
        };

        // Act
        var user = _mapper.Map<User>(dto);

        // Assert
        user.Username.Should().Be(dto.Username);
        user.Email.Should().Be(dto.Email);
    }

    [Fact]
    public void UpdateUserDto_To_User_Mapping()
    {
        // Arrange
        var dto = new UpdateUserDto
        {
            Username = "updated",
            Email = "updated@test.com"
        };

        // Act
        var user = _mapper.Map<User>(dto);

        // Assert
        user.Username.Should().Be(dto.Username);
        user.Email.Should().Be(dto.Email);
    }

    [Fact]
    public void User_To_UserResponseDto_Mapping()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "john",
            Email = "john@test.com",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var dto = _mapper.Map<UserResponseDto>(user);

        // Assert
        dto.Id.Should().Be(user.Id);
        dto.Username.Should().Be(user.Username);
        dto.Email.Should().Be(user.Email);
        dto.CreatedAt.Should().Be(user.CreatedAt);
    }

    [Fact]
    public void CreateCategoryDto_To_Category_Mapping()
    {
        // Arrange
        var dto = new CreateCategoryDto
        {
            Name = "IT",
            Description = "Information Technology"
        };

        // Act
        var category = _mapper.Map<Category>(dto);

        // Assert
        category.Name.Should().Be(dto.Name);
        category.Description.Should().Be(dto.Description);
    }

    [Fact]
    public void UpdateCategoryDto_To_Category_Mapping()
    {
        // Arrange
        var dto = new UpdateCategoryDto
        {
            Name = "Updated",
            Description = "New description"
        };

        // Act
        var category = _mapper.Map<Category>(dto);

        // Assert
        category.Name.Should().Be(dto.Name);
        category.Description.Should().Be(dto.Description);
    }

    [Fact]
    public void Category_To_CategoryResponseDto_Mapping()
    {
        // Arrange
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Finance",
            Description = "Financial docs",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var dto = _mapper.Map<CategoryResponseDto>(category);

        // Assert
        dto.Id.Should().Be(category.Id);
        dto.Name.Should().Be(category.Name);
        dto.Description.Should().Be(category.Description);
        dto.CreatedAt.Should().Be(category.CreatedAt);
    }

    [Fact]
    public void CreateArchiveLogDto_To_ArchiveLog_Mapping()
    {
        // Arrange
        var dto = new CreateArchiveLogDto
        {
            Action = "Created",
            ActionType = ActionType.Created,
            IsCritical = true,
            UserId = Guid.NewGuid(),
            DocumentId = Guid.NewGuid()
        };

        // Act
        var log = _mapper.Map<ArchiveLog>(dto);

        // Assert
        log.Action.Should().Be(dto.Action);
        log.ActionType.Should().Be(dto.ActionType);
        log.IsCritical.Should().Be(dto.IsCritical);
        log.UserId.Should().Be(dto.UserId);
        log.DocumentId.Should().Be(dto.DocumentId);
    }

    [Fact]
    public void ArchiveLog_To_ArchiveLogResponseDto_Mapping()
    {
        // Arrange
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

        // Act
        var dto = _mapper.Map<ArchiveLogResponseDto>(log);

        // Assert
        dto.Id.Should().Be(log.Id);
        dto.Action.Should().Be(log.Action);
        dto.ActionType.Should().Be(log.ActionType);
        dto.IsCritical.Should().Be(log.IsCritical);
        dto.Timestamp.Should().Be(log.Timestamp);
        dto.UserName.Should().Be("john");
        dto.DocumentTitle.Should().Be("Report");
    }

    [Fact]
    public void ArchiveLog_To_ArchiveLogListItemDto_Mapping()
    {
        // Arrange
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

        // Act
        var dto = _mapper.Map<ArchiveLogListItemDto>(log);

        // Assert
        dto.Id.Should().Be(log.Id);
        dto.Action.Should().Be(log.Action);
        dto.ActionType.Should().Be(log.ActionType);
        dto.IsCritical.Should().Be(log.IsCritical);
        dto.Timestamp.Should().Be(log.Timestamp);
        dto.UserName.Should().Be("alice");
    }
}