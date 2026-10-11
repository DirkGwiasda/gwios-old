using GwiOS.Core.Haushaltsbuch.Domain.Workflows.MonthOverview.Contracts.Models;

namespace GwiOS.WebUI.Pdf.Contracts;

/// <summary>
/// Turns the overview of one month into a printable PDF document.
/// </summary>
public interface IMonthOverviewPdfRenderer
{
    /// <summary>
    /// Returns the PDF document of the given overview: the income, then the expenses, each as category tree with the
    /// amount booked directly in each category and the amount including its sub-categories, then the balance.
    /// </summary>
    /// <param name="overview">The overview to render.</param>
    /// <param name="createdAt">The point in time printed as creation time, in the time zone to show it in.</param>
    byte[] Render(MonthOverviewData overview, DateTimeOffset createdAt);
}
