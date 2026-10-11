using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;

namespace GwiOS.Core.Haushaltsbuch.Domain.Workflows.MonthOverview.Contracts.Models;

/// <summary>
/// One category in a <see cref="MonthOverviewData"/>, with the sums of its bookings in the month.
/// </summary>
public sealed class MonthOverviewNode
{
    /// <summary>
    /// The category.
    /// </summary>
    public required Category Category { get; init; }

    /// <summary>
    /// The sum of the bookings in the category itself, without its sub-categories; 0 if it has none.
    /// </summary>
    public required decimal OwnAmount { get; init; }

    /// <summary>
    /// <see cref="OwnAmount"/> plus the <see cref="TotalAmount"/> of all <see cref="Children"/>.
    /// </summary>
    public required decimal TotalAmount { get; init; }

    /// <summary>
    /// How many bookings fall into the category itself, without its sub-categories.
    /// </summary>
    public required int BookingCount { get; init; }

    /// <summary>
    /// The sub-categories with bookings in or below them, ordered by name; empty for none.
    /// </summary>
    public required IReadOnlyList<MonthOverviewNode> Children { get; init; }
}
