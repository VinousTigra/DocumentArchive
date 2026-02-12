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
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void MappingProfile_Configuration_IsValid()
    {
        // Act & Assert
        _mapper.ConfigurationProvider.AssertConfigurationIsValid();
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
}