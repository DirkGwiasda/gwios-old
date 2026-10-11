using GwiOS.Core.Haushaltsbuch.Domain.Workflows.MonthOverview.Contracts.Models;

namespace GwiOS.Core.Haushaltsbuch.Domain.Workflows.MonthOverview.Contracts;

/// <summary>
/// Sums up the bookings of one month along the category tree, like a balance sheet: income first, then expenses.
/// </summary>
public interface IMonthOverviewWorkflow
{
    /// <summary>
    /// Returns the overview of the month of <paramref name="month"/> (any day of it). Every booking of the month
    /// counts, booked and pending alike. Each category shows the sum of its own bookings and, rolled up, the sum
    /// including all of its sub-categories; categories without bookings in themselves or below them are left out.
    /// </summary>
    Task<MonthOverviewData> GetMonthOverviewAsync(DateOnly month, CancellationToken cancellationToken = default);
}
