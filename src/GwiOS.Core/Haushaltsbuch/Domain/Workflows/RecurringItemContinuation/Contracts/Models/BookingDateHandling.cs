namespace GwiOS.Core.Haushaltsbuch.Domain.Workflows.RecurringItemContinuation.Contracts.Models;

/// <summary>
/// How continuing a recurring item treats the dates of the bookings it adjusts.
/// </summary>
public enum BookingDateHandling
{
    /// <summary>
    /// Every adjusted booking keeps its date, even if the successor no longer falls due on it.
    /// </summary>
    KeepDates,

    /// <summary>
    /// Adjusted bookings follow the schedule of the successor: a booking not dated on a due date of the successor is
    /// moved to a free due date in its month, or deleted if its month has no free due date left.
    /// </summary>
    FollowSchedule
}
