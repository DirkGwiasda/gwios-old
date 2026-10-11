using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts.Models;

namespace GwiOS.Core.Haushaltsbuch.Domain.Workflows.RecurringItemContinuation.Contracts.Models;

/// <summary>
/// A booking that does not fit the schedule of the successor of a continued recurring item, and what following the
/// schedule would do with it.
/// </summary>
public sealed class BookingRescheduling
{
    /// <summary>
    /// The booking as it is stored now.
    /// </summary>
    public required Booking Booking { get; init; }

    /// <summary>
    /// The due date of the successor the booking would be moved to, or <c>null</c> if it would be deleted because
    /// its month has no free due date of the successor.
    /// </summary>
    public required DateOnly? NewDate { get; init; }
}
