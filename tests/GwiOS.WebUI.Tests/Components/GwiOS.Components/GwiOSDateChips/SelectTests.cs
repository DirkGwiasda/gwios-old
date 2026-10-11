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

    private IRenderedComponent<GwiOSDateChipsUnderTest> RenderChips()
        => Render<GwiOSDateChipsUnderTest>(parameters => parameters
            .Add(component => component.Value, Today)
            .Add(component => component.ValueChanged, (DateOnly date) => _reportedDates.Add(date))
            .Add(component => component.Today, Today));
}
