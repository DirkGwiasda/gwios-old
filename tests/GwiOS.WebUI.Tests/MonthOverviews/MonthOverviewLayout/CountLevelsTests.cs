using GwiOS.WebUI.Tests.TestInfrastructure;
using MonthOverviewLayoutUnderTest = GwiOS.WebUI.MonthOverviews.MonthOverviewLayout;

namespace GwiOS.WebUI.Tests.MonthOverviews.MonthOverviewLayout;

/// <summary>
/// Covers <c>MonthOverviewLayout.CountLevels</c>.
/// </summary>
public sealed class CountLevelsTests
{
    [Fact]
    public void CountsTheDeepestLevelOfIncomeAndExpenses()
    {
        Assert.Equal(3, MonthOverviewLayoutUnderTest.CountLevels(TestMonthOverviews.CreateOctober()));
    }

    [Fact]
    public void NeedsOneColumnForTheTotals_WhenTheMonthHasNoBookings()
    {
        Assert.Equal(1, MonthOverviewLayoutUnderTest.CountLevels(TestMonthOverviews.CreateEmpty(new DateOnly(2026, 10, 1))));
    }
}
