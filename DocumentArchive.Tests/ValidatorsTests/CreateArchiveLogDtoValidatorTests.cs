using DocumentArchive.Core.DTOs.ArchiveLog;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Repositories;
using DocumentArchive.Services.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;

namespace DocumentArchive.Tests.ValidatorsTests;

public class CreateArchiveLogDtoValidatorTests
{
    private readonly Mock<DocumentRepository> _documentRepoMock;
    private readonly Mock<UserRepository> _userRepoMock;
    private readonly CreateArchiveLogDtoValidator _validator;

    public CreateArchiveLogDtoValidatorTests()
    {
        _documentRepoMock = new Mock<DocumentRepository>();
        _userRepoMock = new Mock<UserRepository>();
        _validator = new CreateArchiveLogDtoValidator(_documentRepoMock.Object, _userRepoMock.Object);
    }

    [Fact]
    public async Task Should_HaveError_When_Action_IsEmpty()
    {
        // Arrange
        var dto = new CreateArchiveLogDto { Action = "" };

        // Act
        var result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Action);
    }

    [Fact]
    public async Task Should_HaveError_When_ActionType_IsInvalid()
    {
        // Arrange
        var dto = new CreateArchiveLogDto { Action = "Test", ActionType = (ActionType)999 };

        // Act
        var result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ActionType);
    }

    [Fact]
    public async Task Should_HaveError_When_DocumentId_DoesNotExist()
    {
        // Arrange
        var docId = Guid.NewGuid();
        _documentRepoMock.Setup(x => x.GetByIdAsync(docId))
            .ReturnsAsync((Document?)null);
        var dto = new CreateArchiveLogDto { DocumentId = docId };

        // Act
        var result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DocumentId);
    }

    [Fact]
    public async Task Should_HaveError_When_UserId_DoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepoMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);
        var dto = new CreateArchiveLogDto { UserId = userId };

        // Act
        var result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
}