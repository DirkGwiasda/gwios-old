using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Models;

namespace GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts;

/// <summary>
/// Works out on which days a recurring item falls due and when it is in effect.
/// </summary>
public interface IRecurringItemScheduler
{
    /// <summary>
    /// Returns the days in the month of <paramref name="month"/> on which the given recurring item falls due, in
    /// ascending order; empty if it does not fall due in that month. Days outside the item's validity
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
