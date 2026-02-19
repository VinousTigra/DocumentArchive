using DocumentArchive.Core.DTOs.DocumentVersion;
using DocumentArchive.Services.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace DocumentArchive.Tests.ValidatorsTests;

public class UpdateDocumentVersionDtoValidatorTests
{
    private readonly UpdateDocumentVersionDtoValidator _validator;

    public UpdateDocumentVersionDtoValidatorTests()
    {
        _validator = new UpdateDocumentVersionDtoValidator();
    }

    [Fact]
    public async Task Should_NotHaveError_When_Comment_IsNull()
    {
        var dto = new UpdateDocumentVersionDto { Comment = null };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Comment);
    }

    [Fact]
    public async Task Should_HaveError_When_Comment_ExceedsMaxLength()
    {
        var dto = new UpdateDocumentVersionDto { Comment = new string('a', 501) };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Comment);
    }
}