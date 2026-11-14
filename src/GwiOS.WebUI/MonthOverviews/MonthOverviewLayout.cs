using GwiOS.Core.Haushaltsbuch.Domain.Workflows.MonthOverview.Contracts.Models;

namespace GwiOS.WebUI.MonthOverviews;

/// <summary>
/// Lays out a month overview in stepped form, as the dialog and the PDF show it: one amount column per level, the
/// root categories in the rightmost column, their sub-categories one column further left, and so on, so that each
/// level stands alone in its column. A category with sub-categories and bookings of its own gets an extra line
/// "(direkt)" on the level of its sub-categories, so that every column adds up to the amount one level up.
/// </summary>
internal static class MonthOverviewLayout
{
    private const string FallbackColor = "#7c93b3";

    /// <summary>
    /// Returns the lines of the given root categories in tree order: each category, then its sub-categories, then
    /// its direct share if it has one.
    /// </summary>
    public static List<MonthOverviewRow> CreateRows(IReadOnlyList<MonthOverviewNode> roots)
    {
        List<MonthOverviewRow> rows = [];
        foreach (MonthOverviewNode root in roots)
        {
            Append(root, 0, root.Category.Color ?? FallbackColor, rows);
        }

        return rows;
    }

    /// <summary>
    /// Returns how many amount columns the overview needs: one per level used in income or expenses, at least one
    /// for the totals.
    /// </summary>
    public static int CountLevels(MonthOverviewData overview)
        => CreateRows([.. overview.Incomes, .. overview.Expenses])
            .Select(row => row.Depth + 1)
            .DefaultIfEmpty(1)
            .Max();

    private static void Append(MonthOverviewNode node, int depth, string color, List<MonthOverviewRow> rows)
    {
        rows.Add(new MonthOverviewRow(
            node.Category.Id, node.Category.Name, depth, node.TotalAmount, color, IsDirectShare: false));
        foreach (MonthOverviewNode child in node.Children)
        {
            Append(child, depth + 1, color, rows);
        }

        if ((node.Children.Count > 0) && (node.BookingCount > 0))
        {
            rows.Add(new MonthOverviewRow(
                node.Category.Id,
                $"{node.Category.Name} (direkt)",
                depth + 1,
                node.OwnAmount,
                color,
                IsDirectShare: true));
        }
    }
}
