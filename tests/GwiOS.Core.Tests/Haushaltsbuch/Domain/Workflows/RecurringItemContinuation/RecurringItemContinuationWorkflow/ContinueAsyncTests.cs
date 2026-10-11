using GwiOS.Core.CrossCutting.Logging.Domain.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Exceptions;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Workflows.RecurringItemContinuation.Contracts.Models;

namespace GwiOS.Core.Tests.Haushaltsbuch.Domain.Workflows.RecurringItemContinuation.RecurringItemContinuationWorkflow;

/// <summary>
/// Covers <c>RecurringItemContinuationWorkflow.ContinueAsync</c>.
/// </summary>
public sealed class ContinueAsyncTests
{
    private readonly RecurringItemContinuationWorkflowProbe _probe = new();
    private readonly RecurringItem _raisedInsurance = RecurringItemContinuationWorkflowProbe.CreateInsurance(94.2m);

    [Fact]
    public async Task ContinuesTheRecurringItemAndReturnsTheSuccessor()
    {
        RecurringItemContinuationResult result = await ContinueAsync(new DateOnly(2026, 11, 1));

        Assert.Same(_raisedInsurance, result.Successor);
        Assert.Equal(_probe.Insurance.Id, result.Successor.PredecessorId);
        Assert.Equal(new DateOnly(2026, 10, 31), _probe.Insurance.ValidUntil);
    }

    [Fact]
    public async Task ChangesBookingsOfThePredecessorFromTheEffectiveDateOnToTheSuccessor()
    {
        _raisedInsurance.CategoryId = Guid.CreateVersion7();
        Booking booking = _probe.AddBooking(new DateOnly(2026, 8, 1), _probe.Insurance.Id);

        await ContinueAsync(new DateOnly(2026, 7, 15));

        Booking adjusted = await _probe.GetStoredBookingAsync(booking.Id);
        Assert.Equal(94.2m, adjusted.Amount);
        Assert.Equal(_raisedInsurance.CategoryId, adjusted.CategoryId);
        Assert.Equal(CategoryType.Expense, adjusted.Type);
        Assert.Equal(_raisedInsurance.Id, adjusted.RecurringItemId);
    }

    [Fact]
    public async Task KeepsDateDescriptionAndStatusOfAnAdjustedBooking()
    {
        Booking booking = _probe.AddBooking(new DateOnly(2026, 8, 1), _probe.Insurance.Id, BookingStatus.Pending);

        await ContinueAsync(new DateOnly(2026, 8, 1));

        Booking adjusted = await _probe.GetStoredBookingAsync(booking.Id);
        Assert.Equal(new DateOnly(2026, 8, 1), adjusted.Date);
        Assert.Equal("Beitrag", adjusted.Description);
        Assert.Equal(BookingStatus.Pending, adjusted.Status);
    }

    [Fact]
    public async Task LeavesBookingsBeforeTheEffectiveDateUnchanged()
    {
        Booking booking = _probe.AddBooking(new DateOnly(2026, 7, 1), _probe.Insurance.Id);

        await ContinueAsync(new DateOnly(2026, 7, 2));

        Booking unchanged = await _probe.GetStoredBookingAsync(booking.Id);
        Assert.Equal(89.5m, unchanged.Amount);
        Assert.Equal(_probe.Insurance.Id, unchanged.RecurringItemId);
    }

    [Fact]
    public async Task LeavesBookingsOfOtherRecurringItemsAndOtherBookingsUnchanged()
    {
        Booking otherItemBooking = _probe.AddBooking(new DateOnly(2026, 8, 1), Guid.CreateVersion7());
        Booking manualBooking = _probe.AddBooking(new DateOnly(2026, 8, 1), recurringItemId: null);

        await ContinueAsync(new DateOnly(2026, 7, 1));

        Assert.Equal(89.5m, (await _probe.GetStoredBookingAsync(otherItemBooking.Id)).Amount);
        Assert.Equal(89.5m, (await _probe.GetStoredBookingAsync(manualBooking.Id)).Amount);
    }

    [Theory]
    [InlineData(2025, 10, 31, false)]
    [InlineData(2025, 11, 1, true)]
    public async Task AdjustsBookingsAtMostTwelveMonthsBackIncludingTheCurrentMonth(
        int year,
        int month,
        int day,
        bool isAdjusted)
    {
        Booking booking = _probe.AddBooking(new DateOnly(year, month, day), _probe.Insurance.Id);

        await ContinueAsync(new DateOnly(2025, 1, 1));

        Assert.Equal(isAdjusted ? 94.2m : 89.5m, (await _probe.GetStoredBookingAsync(booking.Id)).Amount);
    }

    [Fact]
    public async Task AdjustsBookingsAfterTheCurrentMonthAsWell()
    {
        Booking booking = _probe.AddBooking(new DateOnly(2026, 12, 1), _probe.Insurance.Id);

        await ContinueAsync(new DateOnly(2026, 11, 1));

        Assert.Equal(94.2m, (await _probe.GetStoredBookingAsync(booking.Id)).Amount);
    }

    [Fact]
    public async Task ReturnsTheNumberOfAdjustedBookings()
    {
        _probe.AddBooking(new DateOnly(2026, 8, 1), _probe.Insurance.Id);
        _probe.AddBooking(new DateOnly(2026, 9, 1), _probe.Insurance.Id);
        _probe.AddBooking(new DateOnly(2026, 9, 1), recurringItemId: null);

        RecurringItemContinuationResult result = await ContinueAsync(new DateOnly(2026, 8, 1));

        Assert.Equal(2, result.AdjustedBookingCount);
    }

    [Fact]
    public async Task KeepsTheDatesOfBookingsNotOnADueDateOfTheSuccessor_WhenDatesAreToBeKept()
    {
        _raisedInsurance.DueDay = 15;
        Booking booking = _probe.AddBooking(new DateOnly(2026, 8, 1), _probe.Insurance.Id);

        RecurringItemContinuationResult result = await ContinueAsync(new DateOnly(2026, 8, 1));

        Assert.Equal(new DateOnly(2026, 8, 1), (await _probe.GetStoredBookingAsync(booking.Id)).Date);
        Assert.Equal(0, result.MovedBookingCount);
    }

    [Fact]
    public async Task MovesABookingToTheNewDueDayOfItsMonth_WhenTheScheduleIsToBeFollowed()
    {
        _raisedInsurance.DueDay = 15;
        Booking booking = _probe.AddBooking(new DateOnly(2026, 8, 1), _probe.Insurance.Id);

        RecurringItemContinuationResult result = await ContinueAsync(
            new DateOnly(2026, 8, 1),
            BookingDateHandling.FollowSchedule);

        Booking moved = await _probe.GetStoredBookingAsync(booking.Id);
        Assert.Equal(new DateOnly(2026, 8, 15), moved.Date);
        Assert.Equal(94.2m, moved.Amount);
        Assert.Equal(1, result.AdjustedBookingCount);
        Assert.Equal(1, result.MovedBookingCount);
    }

    [Fact]
    public async Task DeletesABookingInAMonthWithoutDueDate_WhenTheScheduleIsToBeFollowed()
    {
        // StartMonth 7 is August (zero-based): from then on due in August and November only.
        _raisedInsurance.Interval = RecurringInterval.Quarterly;
        _raisedInsurance.StartMonth = 7;
        Booking august = _probe.AddBooking(new DateOnly(2026, 8, 1), _probe.Insurance.Id);
        Booking september = _probe.AddBooking(new DateOnly(2026, 9, 1), _probe.Insurance.Id);

        RecurringItemContinuationResult result = await ContinueAsync(
            new DateOnly(2026, 8, 1),
            BookingDateHandling.FollowSchedule);

        List<Booking> stored = await _probe.BookingManager.GetBookingsInRangeAsync(
            DateOnly.MinValue,
            DateOnly.MaxValue,
            TestContext.Current.CancellationToken);
        Assert.Equal([august.Id], stored.Select(booking => booking.Id));
        Assert.Equal(1, result.AdjustedBookingCount);
        Assert.Equal(1, result.DeletedBookingCount);
        Assert.Equal(september.Id, Assert.Single(_probe.BookingManager.DeletedBookingIds));
    }

    [Fact]
    public async Task LeavesBookingsUnchanged_WhenTheContinuationFails()
    {
        Booking booking = _probe.AddBooking(new DateOnly(2026, 8, 1), _probe.Insurance.Id);

        await Assert.ThrowsAsync<RecurringItemNotFoundException>(
            () => _probe.Workflow.ContinueAsync(
                Guid.CreateVersion7(),
                new DateOnly(2026, 8, 1),
                _raisedInsurance,
                BookingDateHandling.KeepDates,
                TestContext.Current.CancellationToken));

        Assert.Equal(89.5m, (await _probe.GetStoredBookingAsync(booking.Id)).Amount);
    }

    [Fact]
    public async Task LogsTheAdjustmentWithBothIdsAndTheNumberOfBookings()
    {
        _probe.AddBooking(new DateOnly(2026, 8, 1), _probe.Insurance.Id);

        await ContinueAsync(new DateOnly(2026, 8, 1));

        LogEntry logEntry = Assert.Single(_probe.Logger.Entries);
        Assert.Equal(LogLevel.Information, logEntry.LogLevel);
        Assert.Equal(_raisedInsurance.Id.ToString(), logEntry.ContextData["RecurringItemId"]);
        Assert.Equal(_probe.Insurance.Id.ToString(), logEntry.ContextData["PredecessorId"]);
        Assert.Equal("1", logEntry.ContextData["AdjustedBookingCount"]);
        Assert.Equal("0", logEntry.ContextData["MovedBookingCount"]);
        Assert.Equal("0", logEntry.ContextData["DeletedBookingCount"]);
    }

    private Task<RecurringItemContinuationResult> ContinueAsync(
        DateOnly effectiveDate,
        BookingDateHandling bookingDateHandling = BookingDateHandling.KeepDates)
        => _probe.Workflow.ContinueAsync(
            _probe.Insurance.Id,
            effectiveDate,
            _raisedInsurance,
            bookingDateHandling,
            TestContext.Current.CancellationToken);
}
