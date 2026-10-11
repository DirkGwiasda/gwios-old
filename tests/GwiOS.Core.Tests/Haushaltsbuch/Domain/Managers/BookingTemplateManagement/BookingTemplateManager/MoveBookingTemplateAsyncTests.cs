using GwiOS.Core.CrossCutting.Logging.Domain.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Exceptions;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Models;

namespace GwiOS.Core.Tests.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.BookingTemplateManager;

/// <summary>
/// Covers <c>BookingTemplateManager.MoveBookingTemplateAsync</c>.
/// </summary>
public sealed class MoveBookingTemplateAsyncTests
{
    private readonly BookingTemplateManagerProbe _probe = new();

    [Fact]
    public async Task SwapsTheTemplateWithTheOneBeforeIt_WhenMovedUp()
    {
        await _probe.StoreAsync("Edeka", 0);
        await _probe.StoreAsync("Bäcker", 1);
        BookingTemplate aral = await _probe.StoreAsync("Aral", 2);

        await _probe.BookingTemplateManager.MoveBookingTemplateAsync(
            aral.Id,
            BookingTemplateMoveDirection.Up,
            TestContext.Current.CancellationToken);

        Assert.Equal(["Edeka", "Aral", "Bäcker"], await _probe.GetStoredDescriptionsAsync());
    }

    [Fact]
    public async Task SwapsTheTemplateWithTheOneAfterIt_WhenMovedDown()
    {
        BookingTemplate edeka = await _probe.StoreAsync("Edeka", 0);
        await _probe.StoreAsync("Bäcker", 1);
        await _probe.StoreAsync("Aral", 2);

        await _probe.BookingTemplateManager.MoveBookingTemplateAsync(
            edeka.Id,
            BookingTemplateMoveDirection.Down,
            TestContext.Current.CancellationToken);

        Assert.Equal(["Bäcker", "Edeka", "Aral"], await _probe.GetStoredDescriptionsAsync());
    }

    [Fact]
    public async Task NumbersAllPositionsAfreshInTheNewOrder()
    {
        await _probe.StoreAsync("Edeka", 3);
        await _probe.StoreAsync("Bäcker", 8);
        BookingTemplate aral = await _probe.StoreAsync("Aral", 15);

        await _probe.BookingTemplateManager.MoveBookingTemplateAsync(
            aral.Id,
            BookingTemplateMoveDirection.Up,
            TestContext.Current.CancellationToken);

        List<BookingTemplate> storedTemplates =
            await _probe.BookingTemplateRepository.GetAllAsync(TestContext.Current.CancellationToken);
        Assert.Equal(["Edeka", "Aral", "Bäcker"], storedTemplates.Select(bookingTemplate => bookingTemplate.Description));
        Assert.Equal([0, 1, 2], storedTemplates.Select(bookingTemplate => bookingTemplate.Position));
        Assert.Equal(3, Assert.Single(_probe.BookingTemplateRepository.PositionUpdates).Count);
    }

    [Fact]
    public async Task ChangesNothing_WhenTheFirstTemplateIsMovedUp()
    {
        BookingTemplate edeka = await _probe.StoreAsync("Edeka", 0);
        await _probe.StoreAsync("Bäcker", 1);

        await _probe.BookingTemplateManager.MoveBookingTemplateAsync(
            edeka.Id,
            BookingTemplateMoveDirection.Up,
            TestContext.Current.CancellationToken);

        Assert.Equal(["Edeka", "Bäcker"], await _probe.GetStoredDescriptionsAsync());
        Assert.Empty(_probe.BookingTemplateRepository.PositionUpdates);
        Assert.Empty(_probe.Logger.Entries);
    }

    [Fact]
    public async Task ChangesNothing_WhenTheLastTemplateIsMovedDown()
    {
        await _probe.StoreAsync("Edeka", 0);
        BookingTemplate baecker = await _probe.StoreAsync("Bäcker", 1);

        await _probe.BookingTemplateManager.MoveBookingTemplateAsync(
            baecker.Id,
            BookingTemplateMoveDirection.Down,
            TestContext.Current.CancellationToken);

        Assert.Equal(["Edeka", "Bäcker"], await _probe.GetStoredDescriptionsAsync());
        Assert.Empty(_probe.BookingTemplateRepository.PositionUpdates);
    }

    [Fact]
    public async Task ThrowsBookingTemplateNotFoundException_WhenTheTemplateIsNotStored()
    {
        Guid unknownId = Guid.CreateVersion7();

        BookingTemplateNotFoundException exception = await Assert.ThrowsAsync<BookingTemplateNotFoundException>(
            () => _probe.BookingTemplateManager.MoveBookingTemplateAsync(
                unknownId,
                BookingTemplateMoveDirection.Up,
                TestContext.Current.CancellationToken));

        Assert.Equal(unknownId, exception.BookingTemplateId);
    }

    [Fact]
    public async Task LogsTheMoveWithTheBookingTemplateId()
    {
        await _probe.StoreAsync("Edeka", 0);
        BookingTemplate baecker = await _probe.StoreAsync("Bäcker", 1);

        await _probe.BookingTemplateManager.MoveBookingTemplateAsync(
            baecker.Id,
            BookingTemplateMoveDirection.Up,
            TestContext.Current.CancellationToken);

        LogEntry logEntry = Assert.Single(_probe.Logger.Entries);
        Assert.Equal(LogLevel.Information, logEntry.LogLevel);
        Assert.Equal("Booking template moved.", logEntry.Message);
        Assert.Equal(baecker.Id.ToString(), logEntry.ContextData["BookingTemplateId"]);
    }
}
