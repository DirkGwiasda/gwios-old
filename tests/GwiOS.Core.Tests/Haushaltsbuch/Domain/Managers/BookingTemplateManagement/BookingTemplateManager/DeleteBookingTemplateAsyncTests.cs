using GwiOS.Core.CrossCutting.Logging.Domain.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Models;

namespace GwiOS.Core.Tests.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.BookingTemplateManager;

/// <summary>
/// Covers <c>BookingTemplateManager.DeleteBookingTemplateAsync</c>.
/// </summary>
public sealed class DeleteBookingTemplateAsyncTests
{
    private readonly BookingTemplateManagerProbe _probe = new();

    [Fact]
    public async Task DeletesOnlyTheTemplateWithTheGivenId()
    {
        BookingTemplate edeka = await _probe.StoreAsync("Edeka", 0);
        await _probe.StoreAsync("Bäcker", 1);

        await _probe.BookingTemplateManager.DeleteBookingTemplateAsync(edeka.Id, TestContext.Current.CancellationToken);

        Assert.Equal(["Bäcker"], await _probe.GetStoredDescriptionsAsync());
    }

    [Fact]
    public async Task Succeeds_WhenTheTemplateIsNotStored()
    {
        Exception? exception = await Record.ExceptionAsync(
            () => _probe.BookingTemplateManager.DeleteBookingTemplateAsync(
                Guid.CreateVersion7(),
                TestContext.Current.CancellationToken));

        Assert.Null(exception);
    }

    [Fact]
    public async Task LogsTheDeletionWithTheBookingTemplateId()
    {
        BookingTemplate edeka = await _probe.StoreAsync("Edeka", 0);

        await _probe.BookingTemplateManager.DeleteBookingTemplateAsync(edeka.Id, TestContext.Current.CancellationToken);

        LogEntry logEntry = Assert.Single(_probe.Logger.Entries);
        Assert.Equal(LogLevel.Information, logEntry.LogLevel);
        Assert.Equal("Booking template deleted.", logEntry.Message);
        Assert.Equal(edeka.Id.ToString(), logEntry.ContextData["BookingTemplateId"]);
    }
}
