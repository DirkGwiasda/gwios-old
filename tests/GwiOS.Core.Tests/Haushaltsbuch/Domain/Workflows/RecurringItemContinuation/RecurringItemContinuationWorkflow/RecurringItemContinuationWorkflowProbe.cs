using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Models;
using GwiOS.Core.Tests.CrossCutting.Logging.Contracts;
using GwiOS.Core.Tests.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts;
using GwiOS.Core.Tests.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts;
using GwiOS.Core.Tests.TestInfrastructure;
using RecurringItemContinuationWorkflowUnderTest =
    GwiOS.Core.Haushaltsbuch.Domain.Workflows.RecurringItemContinuation.RecurringItemContinuationWorkflow;

namespace GwiOS.Core.Tests.Haushaltsbuch.Domain.Workflows.RecurringItemContinuation.RecurringItemContinuationWorkflow;

/// <summary>
/// Runs a <c>RecurringItemContinuationWorkflow</c> against fake managers, a clock standing on 9 October 2026 and a
/// recording logger, and offers a stored monthly insurance of 89.50 € as the item to continue.
/// </summary>
internal sealed class RecurringItemContinuationWorkflowProbe
{
    public RecurringItemContinuationWorkflowProbe()
    {
        Insurance = CreateInsurance(89.5m);
        RecurringItemManager.Add(Insurance);
        Workflow = new RecurringItemContinuationWorkflowUnderTest(RecurringItemManager, BookingManager, TimeProvider, Logger);
    }

    public RecurringItem Insurance { get; }

    public RecurringItemManagerFake RecurringItemManager { get; } = new();

    public BookingManagerFake BookingManager { get; } = new();

    public TimeProviderFake TimeProvider { get; } = new() { UtcNow = new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero) };

    public LoggerFake<RecurringItemContinuationWorkflowUnderTest> Logger { get; } = new();

    public RecurringItemContinuationWorkflowUnderTest Workflow { get; }

    /// <summary>
    /// Returns a new, not stored monthly insurance with the given amount, in a category of its own.
    /// </summary>
    public static RecurringItem CreateInsurance(decimal amount)
        => new()
        {
            Name = "Hausratversicherung",
            Type = CategoryType.Expense,
            CategoryId = Guid.CreateVersion7(),
            Amount = amount,
            Interval = RecurringInterval.Monthly
        };

    /// <summary>
    /// Stores and returns a booking on the given day generated from the given recurring item, or from none.
    /// </summary>
    public Booking AddBooking(DateOnly date, Guid? recurringItemId, BookingStatus status = BookingStatus.Booked)
    {
        Booking booking = new()
        {
            Date = date,
            Type = CategoryType.Expense,
            CategoryId = Insurance.CategoryId,
            Description = "Beitrag",
            Amount = Insurance.Amount,
            Status = status,
            RecurringItemId = recurringItemId
        };
        BookingManager.Add(booking);
        return booking;
    }

    /// <summary>
    /// Returns the stored booking with the given ID.
    /// </summary>
    public async Task<Booking> GetStoredBookingAsync(Guid bookingId)
        => (await BookingManager.GetBookingsInRangeAsync(DateOnly.MinValue, DateOnly.MaxValue))
            .Single(booking => booking.Id == bookingId);
}
