using GwiOSDateChipsUnderTest = GwiOS.WebUI.Components.GwiOS.Components.GwiOSDateChips;

namespace GwiOS.WebUI.Tests.Components.GwiOS.Components.GwiOSDateChips;

/// <summary>
/// Covers how <c>GwiOSDateChips</c> reports the date the user picks.
/// </summary>
public sealed class SelectTests : BunitContext
{
    private static readonly DateOnly Today = new(2026, 10, 11);
    private readonly List<DateOnly> _reportedDates = [];

    [Fact]
    public void ReportsYesterday_WhenGesternIsClicked()
    {
        IRenderedComponent<GwiOSDateChipsUnderTest> chips = RenderChips();

        chips.FindAll("button").Single(button => button.TextContent.Trim() == "Gestern").Click();

        Assert.Equal([new DateOnly(2026, 10, 10)], _reportedDates);
    }

    [Fact]
    public void ReportsTheDatePickedInTheCalendar()
    {
        IRenderedComponent<GwiOSDateChipsUnderTest> chips = RenderChips();

        chips.Find("input[type='date']").Change("2026-09-28");

        Assert.Equal([new DateOnly(2026, 9, 28)], _reportedDates);
    }

    [Fact]
    public void ReportsNothing_WhenTheCalendarFieldIsCleared()
    {
        IRenderedComponent<GwiOSDateChipsUnderTest> chips = RenderChips();

        chips.Find("input[type='date']").Change(string.Empty);

        Assert.Empty(_reportedDates);
    }

    [Fact]
    public void ReportsTheRecentDate_WhenItsChipIsClicked()
    {
        IRenderedComponent<GwiOSDateChipsUnderTest> chips = RenderChips(new DateOnly(2026, 10, 3));

        chips.FindAll("button").Single(button => button.TextContent.Trim() == "Zuletzt 03.10.").Click();

        Assert.Equal([new DateOnly(2026, 10, 3)], _reportedDates);
    }

    private IRenderedComponent<GwiOSDateChipsUnderTest> RenderChips(DateOnly? recentDate = null)
        => Render<GwiOSDateChipsUnderTest>(parameters => parameters
            .Add(component => component.Value, Today)
            .Add(component => component.ValueChanged, (DateOnly date) => _reportedDates.Add(date))
            .Add(component => component.Today, Today)
            .Add(component => component.RecentDate, recentDate));
}
