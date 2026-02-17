using DocumentArchive.Core.DTOs.ArchiveLog;
using DocumentArchive.Core.Interfaces.Repositorys;
using DocumentArchive.Core.Models;
using DocumentArchive.Services.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;

namespace DocumentArchive.Tests.ValidatorsTests;

public class CreateArchiveLogDtoValidatorTests
{
    private readonly Mock<IDocumentRepository> _documentRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly CreateArchiveLogDtoValidator _validator;

    public CreateArchiveLogDtoValidatorTests()
    {
        _documentRepoMock = new Mock<IDocumentRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _validator = new CreateArchiveLogDtoValidator(
            _documentRepoMock.Object,
            _userRepoMock.Object);
    }

    [Fact]
    public async Task Should_HaveError_When_Action_IsEmpty()
    {
        var dto = new CreateArchiveLogDto { Action = "" };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Action);
    }

    [Fact]
    public async Task Should_HaveError_When_ActionType_IsInvalid()
    {
        var dto = new CreateArchiveLogDto { Action = "Test", ActionType = (ActionType)999 };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.ActionType);
    }

    [Fact]
    public async Task Should_HaveError_When_DocumentId_DoesNotExist()
    {
        var docId = Guid.NewGuid();
        _documentRepoMock.Setup(x => x.GetByIdAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);
        var dto = new CreateArchiveLogDto { DocumentId = docId };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.DocumentId);
    }

    [Fact]
    public async Task Should_NotHaveError_When_DocumentId_Exists()
    {
        var docId = Guid.NewGuid();
        _documentRepoMock.Setup(x => x.GetByIdAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Document { Id = docId });
        var dto = new CreateArchiveLogDto
        {
            Action = "Test",
            DocumentId = docId,
            UserId = Guid.NewGuid()
        };
        _userRepoMock.Setup(x => x.GetByIdAsync(dto.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = dto.UserId });
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.DocumentId);
    }

    [Fact]
    public async Task Should_HaveError_When_UserId_DoesNotExist()
    {
        var userId = Guid.NewGuid();
        _userRepoMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        var dto = new CreateArchiveLogDto { UserId = userId };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
}