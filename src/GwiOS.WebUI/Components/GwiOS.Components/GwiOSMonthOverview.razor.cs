using System.Globalization;
using GwiOS.Core.Haushaltsbuch.Domain.Workflows.MonthOverview.Contracts.Models;
using GwiOS.WebUI.MonthOverviews;
using Microsoft.AspNetCore.Components;

namespace GwiOS.WebUI.Components.GwiOS.Components;

/// <summary>
/// Shows the bookings of one month like a balance sheet, but one below the other: first the income, then the
/// expenses, each as the category tree in stepped form (see <see cref="MonthOverviewLayout"/>): the root categories
/// in the rightmost amount column, each level of sub-categories one column further left. Then the totals and the
/// balance, in the rightmost column. If <see cref="OnCategorySelected"/> is set, every line can be clicked, e.g. to
/// show its bookings.
/// </summary>
public partial class GwiOSMonthOverview
{
    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

    private int _levelCount = 1;

    /// <summary>
    /// The overview to show, e.g. from <c>IMonthOverviewWorkflow.GetMonthOverviewAsync</c>.
    /// </summary>
    [Parameter, EditorRequired]
    public MonthOverviewData Overview { get; set; } = default!;

    /// <summary>
    /// Raised with the line the user clicks; or not set, then the lines cannot be clicked. A category line stands for
    /// the category with its sub-categories, a "(direkt)" line only for the bookings in the category itself.
    /// </summary>
    [Parameter]
    public EventCallback<MonthOverviewRow> OnCategorySelected { get; set; }

    private (string Title, IReadOnlyList<MonthOverviewNode> Nodes, decimal Total, string Accent)[] Sections
        =>
        [
            ("Einnahmen", Overview.Incomes, Overview.IncomeTotal, "income"),
            ("Ausgaben", Overview.Expenses, Overview.ExpenseTotal, "expense")
        ];

    /// <inheritdoc />
    protected override void OnParametersSet()
        => _levelCount = MonthOverviewLayout.CountLevels(Overview);

    private static string FormatAmount(decimal value)
        => value.ToString("C", GermanCulture);

    private static string FormatSignedAmount(decimal value)
        => (value >= 0 ? "+ " : "– ") + Math.Abs(value).ToString("C", GermanCulture);
}
