using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Exceptions;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Workflows.RecurringItemContinuation.Contracts.Models;

namespace GwiOS.Core.Haushaltsbuch.Domain.Workflows.RecurringItemContinuation.Contracts;

/// <summary>
/// Continues a recurring item of the Haushaltsbuch with changed values from a given day on, and carries the change
/// over to the bookings generated from it on or after that day.
/// </summary>
public interface IRecurringItemContinuationWorkflow
{
    /// <summary>
    /// Returns, without changing anything, the bookings that <see cref="ContinueAsync"/> would adjust but that are
    /// not dated on a due date of <paramref name="successor"/> as it would be in effect from
    /// <paramref name="effectiveDate"/> on (e.g. because the due day or the interval changes), each with the date it
    /// would be moved to or <c>null</c> if it would be deleted, ordered by date. Bookings are matched to due dates
    /// month by month: a booking already on a due date keeps it, the others take the remaining due dates in order,
    /// and bookings left over are deleted. Empty if every booking fits.
    /// </summary>
    Task<List<BookingRescheduling>> GetBookingReschedulingsAsync(
        Guid predecessorId,
        DateOnly effectiveDate,
        RecurringItem successor,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Continues the recurring item with the ID <paramref name="predecessorId"/> from <paramref name="effectiveDate"/>
    /// on with the values of <paramref name="successor"/> (see <c>IRecurringItemManager.ContinueRecurringItemAsync</c>),
    /// then changes every booking generated from the predecessor that is dated on or after
    /// <paramref name="effectiveDate"/> — but not before the first day of the month eleven months before the current
    /// one, so at most twelve months back including the current month — to the type, category and amount of the
    /// successor and refers it to the successor. Description and status of the bookings stay as they are; their dates
    /// stay as well with <see cref="BookingDateHandling.KeepDates"/>, while with
    /// <see cref="BookingDateHandling.FollowSchedule"/> the bookings listed by <see cref="GetBookingReschedulingsAsync"/>
    /// are moved or deleted as listed there. Throws a <see cref="RecurringItemNotFoundException"/>, <see cref="RecurringItemNotContinuableException"/> or
    /// <see cref="RecurringItemValidationException"/> as the continuation does; bookings are then left unchanged.
    /// </summary>
    Task<RecurringItemContinuationResult> ContinueAsync(
        Guid predecessorId,
        DateOnly effectiveDate,
        RecurringItem successor,
        BookingDateHandling bookingDateHandling,
        CancellationToken cancellationToken = default);
}
