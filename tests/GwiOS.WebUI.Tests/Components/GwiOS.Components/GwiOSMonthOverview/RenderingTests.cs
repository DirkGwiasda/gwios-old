using GwiOS.WebUI.Tests.TestInfrastructure;
using GwiOSMonthOverviewUnderTest = GwiOS.WebUI.Components.GwiOS.Components.GwiOSMonthOverview;

namespace GwiOS.WebUI.Tests.Components.GwiOS.Components.GwiOSMonthOverview;

/// <summary>
/// Covers how <c>GwiOSMonthOverview</c> shows the income and expenses of a month along the category tree.
/// </summary>
public sealed class RenderingTests : BunitContext
{
    [Fact]
    public void ShowsIncomeFirstThenExpenses()
    {
        IRenderedComponent<GwiOSMonthOverviewUnderTest> overview = RenderOctober();

        Assert.Equal(
            ["Einnahmen", "Ausgaben"],
            overview.FindAll(".gwios-month-overview-title").Select(title => title.TextContent));
    }

    [Fact]
    public void ShowsEachLevelInItsOwnColumn_RootCategoriesRightmost()
    {
        IRenderedComponent<GwiOSMonthOverviewUnderTest> overview = RenderOctober();

        // Columns after the name: level 2, level 1, level 0 (rightmost).
        Assert.Equal(
            [
                "Gehalt |  |  | 3.000,00 €",
                "Haushalt |  |  | 60,00 €",
                "Lebensmittel |  | 50,00 € | ",
                "Bio | 2,50 € |  | ",
                "Lebensmittel (direkt) | 47,50 € |  | ",
                "Haushalt (direkt) |  | 10,00 € | ",
                "Mobilität |  |  | 60,00 €"
            ],
            overview.FindAll(".gwios-month-overview-row:has(.gwios-month-overview-name)").Select(row => string.Join(
                " | ",
                row.Children.Select(cell => cell.TextContent.Trim()))));
    }

    [Fact]
    public void SetsTheNumberOfAmountColumns()
    {
        IRenderedComponent<GwiOSMonthOverviewUnderTest> overview = RenderOctober();

        Assert.Equal("--gwios-month-overview-levels: 3;", overview.Find(".gwios-month-overview").GetAttribute("style"));
    }

    [Fact]
    public void MarksTheDirectShareOfACategory()
    {
        IRenderedComponent<GwiOSMonthOverviewUnderTest> overview = RenderOctober();

        Assert.Equal(
            ["Lebensmittel (direkt)", "Haushalt (direkt)"],
            overview.FindAll(".gwios-month-overview-row--direct .gwios-month-overview-name").Select(name => name.TextContent.Trim()));
    }

    [Fact]
    public void ShowsTheTotalsAndTheBalance()
    {
        IRenderedComponent<GwiOSMonthOverviewUnderTest> overview = RenderOctober();

        Assert.Equal(
            ["Summe Einnahmen |  |  | 3.000,00 €", "Summe Ausgaben |  |  | 120,00 €"],
            overview.FindAll(".gwios-month-overview-row--total").Select(row => string.Join(
                " | ",
                row.Children.Select(cell => cell.TextContent.Trim()))));
        Assert.Contains("+ 2.880,00 €", overview.Find(".gwios-month-overview-row--balance").TextContent);
    }

    [Fact]
    public void SaysSo_WhenTheMonthHasNoBookings()
    {
        IRenderedComponent<GwiOSMonthOverviewUnderTest> overview = Render<GwiOSMonthOverviewUnderTest>(parameters => parameters
            .Add(component => component.Overview, TestMonthOverviews.CreateEmpty(new DateOnly(2026, 10, 1))));

        Assert.Equal(
            ["Keine Einnahmen in diesem Monat.", "Keine Ausgaben in diesem Monat."],
            overview.FindAll(".gwios-month-overview-empty").Select(note => note.TextContent));
    }

    private IRenderedComponent<GwiOSMonthOverviewUnderTest> RenderOctober()
        => Render<GwiOSMonthOverviewUnderTest>(parameters => parameters
            .Add(component => component.Overview, TestMonthOverviews.CreateOctober()));
}
