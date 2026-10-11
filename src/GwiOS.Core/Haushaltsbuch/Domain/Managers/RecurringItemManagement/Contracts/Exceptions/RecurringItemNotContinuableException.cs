namespace GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Exceptions;

/// <summary>
/// Thrown when a recurring item is to be continued from a day on which it cannot be continued: it already has an end
/// (it was continued before or ends anyway), or it does not start before that day.
/// </summary>
/// <param name="recurringItemId">The ID of the recurring item that cannot be continued.</param>
/// <param name="effectiveDate">The day the recurring item was to be continued from.</param>
public sealed class RecurringItemNotContinuableException(Guid recurringItemId, DateOnly effectiveDate)
    : Exception(
        $"The recurring item with ID '{recurringItemId}' cannot be continued from {effectiveDate:O}: "
        + "it already has an end or does not start before that day.")
{
    /// <summary>
    /// The ID of the recurring item that cannot be continued.
    /// </summary>
    public Guid RecurringItemId { get; } = recurringItemId;

    /// <summary>
    /// The day the recurring item was to be continued from.
    /// </summary>
    public DateOnly EffectiveDate { get; } = effectiveDate;
}
