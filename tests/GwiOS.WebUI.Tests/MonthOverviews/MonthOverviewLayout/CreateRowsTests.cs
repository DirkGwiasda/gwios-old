using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Workflows.MonthOverview.Contracts.Models;
using GwiOS.WebUI.MonthOverviews;
using GwiOS.WebUI.Tests.TestInfrastructure;
using MonthOverviewLayoutUnderTest = GwiOS.WebUI.MonthOverviews.MonthOverviewLayout;

namespace GwiOS.WebUI.Tests.MonthOverviews.MonthOverviewLayout;

/// <summary>
/// Covers <c>MonthOverviewLayout.CreateRows</c>.
/// </summary>
public sealed class CreateRowsTests
{
    [Fact]
    public void ListsEachCategoryWithItsDepthAndRolledUpAmount_ThenItsDirectShareBelowItsSubCategories()
    {
        MonthOverviewData october = TestMonthOverviews.CreateOctober();

        List<MonthOverviewRow> rows = MonthOverviewLayoutUnderTest.CreateRows(october.Expenses);

        Assert.Equal(
            [
                ("Haushalt", 0, 60m, false),
                ("Lebensmittel", 1, 50m, false),
                ("Bio", 2, 2.5m, false),
                ("Lebensmittel (direkt)", 2, 47.5m, true),
                ("Haushalt (direkt)", 1, 10m, true),
                ("Mobilität", 0, 60m, false)
            ],
            rows.Select(row => (row.Name, row.Depth, row.Amount, row.IsDirectShare)));
    }

    [Fact]
    public void AddsNoDirectShare_ForACategoryWithoutOwnBookings()
    {
        Category household = new() { Name = "Haushalt", Type = CategoryType.Expense, Color = "#ffd056" };
        Category groceries = new() { Name = "Lebensmittel", Type = CategoryType.Expense, ParentId = household.Id };
        MonthOverviewNode root = TestMonthOverviews.CreateNode(
            household, 0m, 50m, 0, [TestMonthOverviews.CreateNode(groceries, 50m, 50m, 2, [])]);

        List<MonthOverviewRow> rows = MonthOverviewLayoutUnderTest.CreateRows([root]);

        Assert.Equal(["Haushalt", "Lebensmittel"], rows.Select(row => row.Name));
    }

    [Fact]
    public void GivesEveryLineTheColorOfItsRootCategory()
    {
        List<MonthOverviewRow> rows = MonthOverviewLayoutUnderTest.CreateRows(TestMonthOverviews.CreateOctober().Expenses);

        Assert.Equal(["#ffd056", "#ffd056", "#ffd056", "#ffd056", "#ffd056", "#3399ff"], rows.Select(row => row.Color));
    }
}
