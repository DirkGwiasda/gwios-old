using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Models;

namespace GwiOS.Core.Haushaltsbuch.Domain.Workflows.RecurringItemContinuation.Contracts.Models;

/// <summary>
/// The outcome of continuing a recurring item.
/// </summary>
public sealed class RecurringItemContinuationResult
{
    /// <summary>
    /// The stored successor that continues the recurring item.
    /// </summary>
    public required RecurringItem Successor { get; init; }

    /// <summary>
    /// How many bookings generated from the predecessor were changed to the successor; 0 if there were none.
    /// </summary>
    public required int AdjustedBookingCount { get; init; }

    /// <summary>
    /// How many of the adjusted bookings were moved to a due date of the successor; 0 if their dates were kept.
    /// </summary>
    public required int MovedBookingCount { get; init; }

    /// <summary>
    /// How many bookings were deleted because their month has no free due date of the successor; 0 if their dates
    /// were kept. Deleted bookings are not counted in <see cref="AdjustedBookingCount"/>.
    /// </summary>
    public required int DeletedBookingCount { get; init; }
}
