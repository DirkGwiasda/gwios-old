using AngleSharp.Dom;
using GwiOSDateChipsUnderTest = GwiOS.WebUI.Components.GwiOS.Components.GwiOSDateChips;

namespace GwiOS.WebUI.Tests.Components.GwiOS.Components.GwiOSDateChips;

/// <summary>
/// Covers how <c>GwiOSDateChips</c> marks the selected date.
/// </summary>
public sealed class RenderingTests : BunitContext
{
    private static readonly DateOnly Today = new(2026, 10, 11);

    [Theory]
    [InlineData(0, "Heute")]
    [InlineData(1, "Gestern")]
    [InlineData(2, "Vorgestern")]
    public void MarksTheRelativeDayChip_WhenItsDateIsSelected(int daysAgo, string expectedLabel)
    {
        IRenderedComponent<GwiOSDateChipsUnderTest> chips = RenderChips(Today.AddDays(-daysAgo));

        IElement selectedChip = Assert.Single(chips.FindAll(".gwios-chip--selected"));
        Assert.Equal(expectedLabel, selectedChip.TextContent.Trim());
        Assert.Equal("true", selectedChip.GetAttribute("aria-pressed"));
    }

    [Fact]
    public void ShowsAnOlderDateOnTheCalendarChipAndMarksIt()
    {
        IRenderedComponent<GwiOSDateChipsUnderTest> chips = RenderChips(new DateOnly(2026, 10, 5));

        IElement selectedChip = Assert.Single(chips.FindAll(".gwios-chip--selected"));
        Assert.Equal("📅 05.10.2026", selectedChip.TextContent.Trim());
    }

    [Fact]
    public void ShowsOnlyTheCalendarSymbol_WhenARelativeDayIsSelected()
    {
        IRenderedComponent<GwiOSDateChipsUnderTest> chips = RenderChips(Today);

        Assert.Equal("📅", chips.Find(".gwios-date-chips-custom .gwios-chip").TextContent.Trim());
    }

    [Fact]
    public void PassesTheSelectedDateToTheCalendarField()
    {
        IRenderedComponent<GwiOSDateChipsUnderTest> chips = RenderChips(Today);

        Assert.Equal("2026-10-11", chips.Find("input[type='date']").GetAttribute("value"));
    }

    private IRenderedComponent<GwiOSDateChipsUnderTest> RenderChips(DateOnly value)
        => Render<GwiOSDateChipsUnderTest>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.Today, Today));
}
