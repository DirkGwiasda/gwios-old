namespace GwiOS.Core.Haushaltsbuch.Domain.Workflows.MonthOverview.Contracts.Models;

/// <summary>
/// The bookings of one month summed up along the category tree, split into income and expenses.
/// </summary>
public sealed class MonthOverviewData
{
    /// <summary>
    /// The first day of the month.
    /// </summary>
    public required DateOnly Month { get; init; }

    /// <summary>
    /// The root income categories with bookings in or below them, ordered by name; each with its sub-categories.
    /// </summary>
    public required IReadOnlyList<MonthOverviewNode> Incomes { get; init; }

    /// <summary>
    /// The root expense categories with bookings in or below them, ordered by name; each with its sub-categories.
    /// </summary>
    public required IReadOnlyList<MonthOverviewNode> Expenses { get; init; }

    /// <summary>
    /// The sum of all bookings of the month in income categories.
    /// </summary>
    public required decimal IncomeTotal { get; init; }

    /// <summary>
    /// The sum of all bookings of the month in expense categories.
    /// </summary>
    public required decimal ExpenseTotal { get; init; }

    /// <summary>
    /// <see cref="IncomeTotal"/> minus <see cref="ExpenseTotal"/>.
    /// </summary>
    public required decimal Balance { get; init; }
}
