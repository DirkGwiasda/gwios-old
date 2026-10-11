using GwiOS.Core.CrossCutting.Logging.Domain.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Exceptions;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Models;

namespace GwiOS.Core.Tests.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.BookingTemplateManager;

/// <summary>
/// Covers <c>BookingTemplateManager.CreateBookingTemplateAsync</c>.
/// </summary>
public sealed class CreateBookingTemplateAsyncTests
{
    private readonly BookingTemplateManagerProbe _probe = new();
    private readonly BookingTemplate _bookingTemplate = new() { Description = "Edeka", CategoryId = Guid.CreateVersion7() };

    [Fact]
    public async Task StoresAndReturnsTheBookingTemplate_WhenItIsValid()
    {
        BookingTemplate createdTemplate = await _probe.BookingTemplateManager.CreateBookingTemplateAsync(
            _bookingTemplate,
            TestContext.Current.CancellationToken);

        Assert.Same(_bookingTemplate, createdTemplate);
        Assert.Equal(["Edeka"], await _probe.GetStoredDescriptionsAsync());
    }

    [Fact]
    public async Task PlacesTheFirstBookingTemplateAtPositionZero()
    {
        _bookingTemplate.Position = 7;

        await _probe.BookingTemplateManager.CreateBookingTemplateAsync(
            _bookingTemplate,
            TestContext.Current.CancellationToken);

        Assert.Equal(0, _bookingTemplate.Position);
    }

    [Fact]
    public async Task PlacesTheBookingTemplateBehindAllStoredOnes()
    {
        await _probe.StoreAsync("Bäcker", 4);
        await _probe.StoreAsync("Aral", 2);

        await _probe.BookingTemplateManager.CreateBookingTemplateAsync(
            _bookingTemplate,
            TestContext.Current.CancellationToken);

        Assert.Equal(5, _bookingTemplate.Position);
        Assert.Equal(["Aral", "Bäcker", "Edeka"], await _probe.GetStoredDescriptionsAsync());
    }

    [Fact]
    public async Task ValidatesTheBookingTemplateAgainstTheStoredOnes()
    {
        BookingTemplate storedTemplate = await _probe.StoreAsync("Bäcker", 0);

        await _probe.BookingTemplateManager.CreateBookingTemplateAsync(
            _bookingTemplate,
            TestContext.Current.CancellationToken);

        Assert.Same(_bookingTemplate, Assert.Single(_probe.BookingTemplateValidator.ValidatedBookingTemplates));
        Assert.Same(storedTemplate, Assert.Single(_probe.BookingTemplateValidator.LastExistingBookingTemplates!));
    }

    [Fact]
    public async Task ThrowsBookingTemplateValidationExceptionWithTheValidationErrors_WhenTheTemplateIsInvalid()
    {
        _probe.BookingTemplateValidator.ValidationErrors = ["First error.", "Second error."];

        BookingTemplateValidationException exception = await Assert.ThrowsAsync<BookingTemplateValidationException>(
            () => _probe.BookingTemplateManager.CreateBookingTemplateAsync(
                _bookingTemplate,
                TestContext.Current.CancellationToken));

        Assert.Equal(_bookingTemplate.Id, exception.BookingTemplateId);
        Assert.Equal(["First error.", "Second error."], exception.ValidationErrors);
    }

    [Fact]
    public async Task DoesNotStoreTheBookingTemplate_WhenItIsInvalid()
    {
        _probe.BookingTemplateValidator.ValidationErrors = ["Error."];

        await Assert.ThrowsAsync<BookingTemplateValidationException>(
            () => _probe.BookingTemplateManager.CreateBookingTemplateAsync(
                _bookingTemplate,
                TestContext.Current.CancellationToken));

        Assert.Empty(await _probe.GetStoredDescriptionsAsync());
    }

    [Fact]
    public async Task LogsTheCreationWithTheBookingTemplateId()
    {
        await _probe.BookingTemplateManager.CreateBookingTemplateAsync(
            _bookingTemplate,
            TestContext.Current.CancellationToken);

        LogEntry logEntry = Assert.Single(_probe.Logger.Entries);
        Assert.Equal(LogLevel.Information, logEntry.LogLevel);
        Assert.Equal("Booking template created.", logEntry.Message);
        Assert.Equal(_bookingTemplate.Id.ToString(), logEntry.ContextData["BookingTemplateId"]);
    }
}
