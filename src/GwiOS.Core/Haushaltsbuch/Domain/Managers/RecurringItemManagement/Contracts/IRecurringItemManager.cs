using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Exceptions;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Models;

namespace GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts;

/// <summary>
/// Manages the recurring items of the Haushaltsbuch: creates, changes, reads and deletes them, and rejects
/// recurring items that are invalid.
/// </summary>
public interface IRecurringItemManager
{
    /// <summary>
    /// Validates and stores the given new recurring item and returns it. Throws a
    /// <see cref="RecurringItemValidationException"/> if the recurring item is invalid.
    /// </summary>
    Task<RecurringItem> CreateRecurringItemAsync(RecurringItem recurringItem, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates and stores the given changed recurring item. Throws a <see cref="RecurringItemValidationException"/>
    /// if the recurring item is invalid, or a <see cref="RecurringItemNotFoundException"/> if no recurring item with
    /// its ID is stored.
    /// </summary>
    Task UpdateRecurringItemAsync(RecurringItem recurringItem, CancellationToken cancellationToken = default);

    /// <summary>
    /// Continues the stored recurring item with the ID <paramref name="predecessorId"/> from
    /// <paramref name="effectiveDate"/> on with the values of <paramref name="successor"/>: the predecessor stays stored
    /// but ends on the day before, and the successor is stored with the predecessor as
    /// <see cref="RecurringItem.PredecessorId"/>, <paramref name="effectiveDate"/> as
    /// <see cref="RecurringItem.ValidFrom"/> and no end; both changes are stored together or not at all. Returns the
    /// stored successor. Throws a <see cref="RecurringItemNotFoundException"/> if the predecessor is not stored, a
    /// <see cref="RecurringItemNotContinuableException"/> if it already has an end or does not start before
    /// <paramref name="effectiveDate"/>, or a <see cref="RecurringItemValidationException"/> if the successor is invalid.
    /// </summary>
    Task<RecurringItem> ContinueRecurringItemAsync(
        Guid predecessorId,
        DateOnly effectiveDate,
        RecurringItem successor,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all stored recurring items, ordered by type and then by name. The list is empty if no recurring
    /// items are stored.
    /// </summary>
    Task<List<RecurringItem>> GetAllRecurringItemsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the recurring item with the given ID. Does nothing if no such recurring item is stored. If the item
    /// continued a predecessor that ends on the day before the item's start, that predecessor no longer ends, so a
    /// continuation can be undone by deleting the successor.
    /// </summary>
    Task DeleteRecurringItemAsync(Guid recurringItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the days in the month of <paramref name="month"/> on which the given recurring item falls due, in
    /// ascending order; empty if it does not fall due in that month. Weekly items fall due on the 1st, 8th, 15th and
    /// 22nd, quarterly and yearly items from their start month on. Days outside the item's validity
    /// (<see cref="RecurringItem.ValidFrom"/>, <see cref="RecurringItem.ValidUntil"/>) are left out; whether the item
    /// is active is not considered.
    /// </summary>
    IReadOnlyList<DateOnly> GetDueDates(RecurringItem recurringItem, DateOnly month);

    /// <summary>
    /// Returns whether <paramref name="date"/> lies within the validity of the given recurring item
    /// (<see cref="RecurringItem.ValidFrom"/> to <see cref="RecurringItem.ValidUntil"/>, both inclusive, each
    /// unbounded if not set). Whether the item is active is not considered.
    /// </summary>
    bool IsValidOn(RecurringItem recurringItem, DateOnly date);
}
