using GwiOS.Core.CrossCutting.Logging.Domain.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Exceptions;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Models;

namespace GwiOS.Core.Tests.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.BookingTemplateManager;

/// <summary>
/// Covers <c>BookingTemplateManager.UpdateBookingTemplateAsync</c>.
/// </summary>
public sealed class UpdateBookingTemplateAsyncTests
{
    private readonly BookingTemplateManagerProbe _probe = new();

    [Fact]
    public async Task StoresTheChangedDescriptionAndCategory_WhenTheTemplateIsValid()
    {
        BookingTemplate storedTemplate = await _probe.StoreAsync("Bäcker Müller", 0);
        Guid newCategoryId = Guid.CreateVersion7();
        BookingTemplate changedTemplate = new()
        {
            Id = storedTemplate.Id, Description = "Bäcker Müller (Ecke)", CategoryId = newCategoryId
        };

        await _probe.BookingTemplateManager.UpdateBookingTemplateAsync(
            changedTemplate,
            TestContext.Current.CancellationToken);

        BookingTemplate updatedTemplate =
            Assert.Single(await _probe.BookingTemplateRepository.GetAllAsync(TestContext.Current.CancellationToken));
        Assert.Equal("Bäcker Müller (Ecke)", updatedTemplate.Description);
        Assert.Equal(newCategoryId, updatedTemplate.CategoryId);
    }

    [Fact]
    public async Task ValidatesTheBookingTemplateAgainstTheStoredOnes()
    {
        BookingTemplate storedTemplate = await _probe.StoreAsync("Bäcker", 0);

        await _probe.BookingTemplateManager.UpdateBookingTemplateAsync(
            storedTemplate,
            TestContext.Current.CancellationToken);

        Assert.Same(storedTemplate, Assert.Single(_probe.BookingTemplateValidator.ValidatedBookingTemplates));
        Assert.Same(storedTemplate, Assert.Single(_probe.BookingTemplateValidator.LastExistingBookingTemplates!));
    }

    [Fact]
    public async Task ThrowsBookingTemplateValidationExceptionWithTheValidationErrors_WhenTheTemplateIsInvalid()
    {
        BookingTemplate storedTemplate = await _probe.StoreAsync("Bäcker", 0);
        _probe.BookingTemplateValidator.ValidationErrors = ["First error.", "Second error."];

        BookingTemplateValidationException exception = await Assert.ThrowsAsync<BookingTemplateValidationException>(
            () => _probe.BookingTemplateManager.UpdateBookingTemplateAsync(
                storedTemplate,
                TestContext.Current.CancellationToken));

        Assert.Equal(storedTemplate.Id, exception.BookingTemplateId);
        Assert.Equal(["First error.", "Second error."], exception.ValidationErrors);
    }

    [Fact]
    public async Task ThrowsBookingTemplateNotFoundException_WhenTheTemplateIsNotStored()
    {
        BookingTemplate bookingTemplate = new() { Description = "Edeka", CategoryId = Guid.CreateVersion7() };

        await Assert.ThrowsAsync<BookingTemplateNotFoundException>(
            () => _probe.BookingTemplateManager.UpdateBookingTemplateAsync(
                bookingTemplate,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task LogsTheUpdateWithTheBookingTemplateId()
    {
        BookingTemplate storedTemplate = await _probe.StoreAsync("Bäcker", 0);

        await _probe.BookingTemplateManager.UpdateBookingTemplateAsync(
            storedTemplate,
            TestContext.Current.CancellationToken);

        LogEntry logEntry = Assert.Single(_probe.Logger.Entries);
        Assert.Equal(LogLevel.Information, logEntry.LogLevel);
        Assert.Equal("Booking template updated.", logEntry.Message);
        Assert.Equal(storedTemplate.Id.ToString(), logEntry.ContextData["BookingTemplateId"]);
    }
}
