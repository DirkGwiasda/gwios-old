using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Models;

namespace GwiOS.Core.Tests.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.BookingTemplateManager;

/// <summary>
/// Covers <c>BookingTemplateManager.GetAllBookingTemplatesAsync</c>.
/// </summary>
public sealed class GetAllBookingTemplatesAsyncTests
{
    private readonly BookingTemplateManagerProbe _probe = new();

    [Fact]
    public async Task ReturnsAllStoredTemplatesOrderedByPosition()
    {
        await _probe.StoreAsync("Aral", 2);
        await _probe.StoreAsync("Edeka", 0);
        await _probe.StoreAsync("Bäcker", 1);

        List<BookingTemplate> bookingTemplates =
            await _probe.BookingTemplateManager.GetAllBookingTemplatesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["Edeka", "Bäcker", "Aral"], bookingTemplates.Select(bookingTemplate => bookingTemplate.Description));
    }

    [Fact]
    public async Task ReturnsAnEmptyList_WhenNoTemplatesAreStored()
    {
        List<BookingTemplate> bookingTemplates =
            await _probe.BookingTemplateManager.GetAllBookingTemplatesAsync(TestContext.Current.CancellationToken);

        Assert.Empty(bookingTemplates);
    }
}
