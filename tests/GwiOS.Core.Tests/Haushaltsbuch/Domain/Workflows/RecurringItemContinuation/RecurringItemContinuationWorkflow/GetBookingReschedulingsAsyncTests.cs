using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Workflows.RecurringItemContinuation.Contracts.Models;

namespace GwiOS.Core.Tests.Haushaltsbuch.Domain.Workflows.RecurringItemContinuation.RecurringItemContinuationWorkflow;

/// <summary>
/// Covers <c>RecurringItemContinuationWorkflow.GetBookingReschedulingsAsync</c>.
/// </summary>
public sealed class GetBookingReschedulingsAsyncTests
{
    private readonly RecurringItemContinuationWorkflowProbe _probe = new();
    private readonly RecurringItem _raisedInsurance = RecurringItemContinuationWorkflowProbe.CreateInsurance(94.2m);

    [Fact]
    public async Task ReturnsNothing_WhenEveryBookingIsOnADueDateOfTheSuccessor()
    {
        _probe.AddBooking(new DateOnly(2026, 8, 1), _probe.Insurance.Id);

        Assert.Empty(await GetReschedulingsAsync(new DateOnly(2026, 8, 1)));
    }

    [Fact]
    public async Task MovesABookingToTheNewDueDayOfItsMonth()
    {
        _raisedInsurance.DueDay = 15;
        Booking booking = _probe.AddBooking(new DateOnly(2026, 8, 1), _probe.Insurance.Id);

        BookingRescheduling rescheduling = Assert.Single(await GetReschedulingsAsync(new DateOnly(2026, 8, 1)));

        Assert.Equal(booking.Id, rescheduling.Booking.Id);
        Assert.Equal(new DateOnly(2026, 8, 15), rescheduling.NewDate);
    }

    [Fact]
    public async Task DeletesABookingInAMonthWithoutDueDateOfTheSuccessor()
    {
        // StartMonth 7 is August (zero-based): from then on due in August and November only.
        _raisedInsurance.Interval = RecurringInterval.Quarterly;
        _raisedInsurance.StartMonth = 7;
        Booking september = _probe.AddBooking(new DateOnly(2026, 9, 1), _probe.Insurance.Id);

        BookingRescheduling rescheduling = Assert.Single(await GetReschedulingsAsync(new DateOnly(2026, 8, 1)));

        Assert.Equal(september.Id, rescheduling.Booking.Id);
        Assert.Null(rescheduling.NewDate);
    }

    [Fact]
    public async Task KeepsABookingOnADueDateAndDeletesTheBookingsLeftOver_WhenWeeklyBecomesMonthly()
    {
        RecurringItem weekly = RecurringItemContinuationWorkflowProbe.CreateInsurance(20m);
        weekly.Interval = RecurringInterval.Weekly;
        _probe.RecurringItemManager.Add(weekly);
        foreach (int day in new[] { 1, 8, 15, 22 })
        {
            _probe.AddBooking(new DateOnly(2026, 8, day), weekly.Id);
        }

        List<BookingRescheduling> reschedulings = await _probe.Workflow.GetBookingReschedulingsAsync(
            weekly.Id,
            new DateOnly(2026, 8, 1),
            _raisedInsurance,
            TestContext.Current.CancellationToken);

        Assert.Equal([8, 15, 22], reschedulings.Select(rescheduling => rescheduling.Booking.Date.Day));
        Assert.All(reschedulings, rescheduling => Assert.Null(rescheduling.NewDate));
    }

    [Fact]
    public async Task NeverMovesABookingBeforeTheEffectiveDate()
    {
        _raisedInsurance.DueDay = 5;
        _probe.AddBooking(new DateOnly(2026, 8, 20), _probe.Insurance.Id);

        BookingRescheduling rescheduling = Assert.Single(await GetReschedulingsAsync(new DateOnly(2026, 8, 10)));

        Assert.Null(rescheduling.NewDate);
    }

    [Fact]
    public async Task ChangesNothing()
    {
        _raisedInsurance.DueDay = 15;
        Booking booking = _probe.AddBooking(new DateOnly(2026, 8, 1), _probe.Insurance.Id);

        await GetReschedulingsAsync(new DateOnly(2026, 8, 1));

        Assert.Equal(new DateOnly(2026, 8, 1), (await _probe.GetStoredBookingAsync(booking.Id)).Date);
        Assert.Null(_probe.Insurance.ValidUntil);
    }

    private Task<List<BookingRescheduling>> GetReschedulingsAsync(DateOnly effectiveDate)
        => _probe.Workflow.GetBookingReschedulingsAsync(
            _probe.Insurance.Id,
            effectiveDate,
            _raisedInsurance,
            TestContext.Current.CancellationToken);
}
