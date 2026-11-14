using GwiOS.Core.Haushaltsbuch.Domain.Workflows.MonthOverview.Contracts.Models;
using GwiOS.WebUI.MonthOverviews;
using GwiOS.WebUI.Tests.TestInfrastructure;
using GwiOSMonthOverviewUnderTest = GwiOS.WebUI.Components.GwiOS.Components.GwiOSMonthOverview;

namespace GwiOS.WebUI.Tests.Components.GwiOS.Components.GwiOSMonthOverview;

/// <summary>
/// Covers clicking a line of <c>GwiOSMonthOverview</c>.
/// </summary>
public sealed class SelectCategoryTests : BunitContext
{
    private readonly List<MonthOverviewRow> _selectedRows = [];

    [Fact]
    public void ReportsTheClickedLine()
    {
        MonthOverviewData october = TestMonthOverviews.CreateOctober();
        IRenderedComponent<GwiOSMonthOverviewUnderTest> overview = Render<GwiOSMonthOverviewUnderTest>(parameters => parameters
            .Add(component => component.Overview, october)
            .Add(component => component.OnCategorySelected, (MonthOverviewRow row) => _selectedRows.Add(row)));

        overview.FindAll("button.gwios-month-overview-name")
            .Single(button => button.TextContent.Trim() == "Haushalt (direkt)")
            .Click();

        MonthOverviewRow row = Assert.Single(_selectedRows);
        Assert.Equal(october.Expenses[0].Category.Id, row.CategoryId);
        Assert.Equal(("Haushalt (direkt)", true, 10m), (row.Name, row.IsDirectShare, row.Amount));
    }

    [Fact]
    public void OffersNothingToClick_WithoutAHandler()
    {
        IRenderedComponent<GwiOSMonthOverviewUnderTest> overview = Render<GwiOSMonthOverviewUnderTest>(parameters => parameters
            .Add(component => component.Overview, TestMonthOverviews.CreateOctober()));

        Assert.Empty(overview.FindAll("button"));
    }
}
