using GwiOSBookingDescriptionBoxUnderTest = GwiOS.WebUI.Components.GwiOS.Components.GwiOSBookingDescriptionBox;

namespace GwiOS.WebUI.Tests.Components.GwiOS.Components.GwiOSBookingDescriptionBox;

/// <summary>
/// Covers how <c>GwiOSBookingDescriptionBox</c> reports typed text and picked suggestions.
/// </summary>
public sealed class InputTests : GwiOSBookingDescriptionBoxProbe
{
    [Fact]
    public void ReportsEveryKeystroke()
    {
        IRenderedComponent<GwiOSBookingDescriptionBoxUnderTest> box = RenderBox();

        box.Find("input").Input("Ede");

        Assert.Equal(["Ede"], ReportedValues);
    }

    [Fact]
    public void TrimsTheText_WhenTheUserLeavesTheField()
    {
        IRenderedComponent<GwiOSBookingDescriptionBoxUnderTest> box = RenderBox();

        box.Find("input").Change("  Edeka ");

        Assert.Equal(["Edeka"], ReportedValues);
    }

    [Fact]
    public void ReportsTheDescriptionAndThenTheSuggestionAndClosesTheList_WhenASuggestionIsClicked()
    {
        IRenderedComponent<GwiOSBookingDescriptionBoxUnderTest> box = RenderBox();
        box.Find("input").Input("ed");

        box.FindAll("[role='option']")[2].Click();

        Assert.Equal(["ed", "Edeka"], ReportedValues);
        Assert.Equal([EdekaDrugstore], SelectedSuggestions);
        Assert.Empty(box.FindAll("[role='listbox']"));
        Assert.Equal("Edeka", box.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void ClosesTheList_WhenTheFieldLosesTheFocus()
    {
        IRenderedComponent<GwiOSBookingDescriptionBoxUnderTest> box = RenderBox();
        box.Find("input").Input("ed");

        box.Find("input").Blur();

        Assert.Empty(box.FindAll("[role='listbox']"));
        Assert.Empty(SelectedSuggestions);
    }
}
