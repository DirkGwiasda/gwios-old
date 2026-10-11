using GwiOS.Core.CrossCutting.Logging.Domain.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Models;
using GwiOS.Core.Tests.CrossCutting.Logging.Contracts;
using BookingTemplateValidatorUnderTest =
    GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.BookingTemplateValidator;

namespace GwiOS.Core.Tests.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.BookingTemplateValidator;

/// <summary>
/// Covers <c>BookingTemplateValidator.IsValid</c>.
/// </summary>
public sealed class IsValidTests
{
    private readonly LoggerFake<BookingTemplateValidatorUnderTest> _logger = new();
    private readonly BookingTemplateValidatorUnderTest _validator;
    private readonly Guid _groceriesId = Guid.CreateVersion7();

    public IsValidTests()
    {
        _validator = new BookingTemplateValidatorUnderTest(_logger);
    }

    [Fact]
    public void ReturnsTrueWithoutValidationErrors_WhenTheTemplateIsValid()
    {
        BookingTemplate bookingTemplate = new() { Description = "Edeka", CategoryId = _groceriesId };

        bool isValid = _validator.IsValid(bookingTemplate, [], out List<string> validationErrors);

        Assert.True(isValid);
        Assert.Empty(validationErrors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ReturnsFalseWithAnError_WhenTheDescriptionIsEmpty(string description)
    {
        BookingTemplate bookingTemplate = new() { Description = description, CategoryId = _groceriesId };

        bool isValid = _validator.IsValid(bookingTemplate, [], out List<string> validationErrors);

        Assert.False(isValid);
        Assert.Equal(["The description must not be empty."], validationErrors);
    }

    [Fact]
    public void ReturnsFalseWithAnError_WhenTheDescriptionIsLongerThan200Characters()
    {
        BookingTemplate bookingTemplate = new() { Description = new string('a', 201), CategoryId = _groceriesId };

        bool isValid = _validator.IsValid(bookingTemplate, [], out List<string> validationErrors);

        Assert.False(isValid);
        Assert.Equal(["The description must not exceed 200 characters."], validationErrors);
    }

    [Fact]
    public void ReturnsTrue_WhenTheDescriptionHasExactly200Characters()
    {
        BookingTemplate bookingTemplate = new() { Description = new string('a', 200), CategoryId = _groceriesId };

        bool isValid = _validator.IsValid(bookingTemplate, [], out _);

        Assert.True(isValid);
    }

    [Fact]
    public void ReturnsFalseWithAnError_WhenTheCategoryIsMissing()
    {
        BookingTemplate bookingTemplate = new() { Description = "Edeka", CategoryId = Guid.Empty };

        bool isValid = _validator.IsValid(bookingTemplate, [], out List<string> validationErrors);

        Assert.False(isValid);
        Assert.Equal(["The category is required."], validationErrors);
    }

    [Fact]
    public void ReturnsFalseWithAnError_WhenAnotherTemplateUsesTheSameDescriptionWithTheSameCategory()
    {
        BookingTemplate existing = new() { Description = "Edeka", CategoryId = _groceriesId };
        BookingTemplate bookingTemplate = new() { Description = " edeka ", CategoryId = _groceriesId };

        bool isValid = _validator.IsValid(bookingTemplate, [existing], out List<string> validationErrors);

        Assert.False(isValid);
        Assert.Equal(["Another booking template already uses this description with this category."], validationErrors);
    }

    [Fact]
    public void ReturnsTrue_WhenAnotherTemplateUsesTheSameDescriptionWithAnotherCategory()
    {
        BookingTemplate existing = new() { Description = "Edeka", CategoryId = Guid.CreateVersion7() };
        BookingTemplate bookingTemplate = new() { Description = "Edeka", CategoryId = _groceriesId };

        bool isValid = _validator.IsValid(bookingTemplate, [existing], out _);

        Assert.True(isValid);
    }

    [Fact]
    public void ReturnsTrue_WhenTheOnlyTemplateWithTheSameDescriptionAndCategoryIsTheTemplateItself()
    {
        BookingTemplate bookingTemplate = new() { Description = "Edeka", CategoryId = _groceriesId };

        bool isValid = _validator.IsValid(bookingTemplate, [bookingTemplate], out _);

        Assert.True(isValid);
    }

    [Fact]
    public void LogsAWarningWithTheTemplateIdAndTheErrors_WhenTheTemplateIsInvalid()
    {
        BookingTemplate bookingTemplate = new() { Description = "", CategoryId = Guid.Empty };

        _validator.IsValid(bookingTemplate, [], out _);

        LogEntry logEntry = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Warning, logEntry.LogLevel);
        Assert.Equal("Booking template is invalid.", logEntry.Message);
        Assert.Equal(bookingTemplate.Id.ToString(), logEntry.ContextData["BookingTemplateId"]);
        Assert.Equal(
            "The description must not be empty. The category is required.",
            logEntry.ContextData["ValidationErrors"]);
    }

    [Fact]
    public void LogsNothing_WhenTheTemplateIsValid()
    {
        BookingTemplate bookingTemplate = new() { Description = "Edeka", CategoryId = _groceriesId };

        _validator.IsValid(bookingTemplate, [], out _);

        Assert.Empty(_logger.Entries);
    }
}
