using GwiOSBookingDescriptionBoxUnderTest = GwiOS.WebUI.Components.GwiOS.Components.GwiOSBookingDescriptionBox;

namespace GwiOS.WebUI.Tests.Components.GwiOS.Components.GwiOSBookingDescriptionBox;

/// <summary>
/// Covers picking a suggestion of <c>GwiOSBookingDescriptionBox</c> with the keyboard.
/// </summary>
public sealed class KeyDownTests : GwiOSBookingDescriptionBoxProbe
{
    [Fact]
    public void HighlightsNothingAtFirst_SoEnterPicksNothing()
    {
        IRenderedComponent<GwiOSBookingDescriptionBoxUnderTest> box = RenderBox();
        box.Find("input").Input("ed");

        box.Find("input").KeyDown("Enter");

        Assert.Empty(SelectedSuggestions);
        Assert.Empty(box.FindAll(".gwios-description-box-option--highlighted"));
    }

    [Fact]
    public void PicksTheHighlightedSuggestion_WhenEnterIsPressedAfterTheArrowKeys()
    {
        IRenderedComponent<GwiOSBookingDescriptionBoxUnderTest> box = RenderBox();
        box.Find("input").Input("ed");

        box.Find("input").KeyDown("ArrowDown");
        box.Find("input").KeyDown("ArrowDown");
        box.Find("input").KeyDown("ArrowDown");
        box.Find("input").KeyDown("ArrowUp");
        box.Find("input").KeyDown("Enter");

        Assert.Equal([Bakery], SelectedSuggestions);
    }

    [Fact]
    public void PointsTheFieldToTheHighlightedOption()
    {
        IRenderedComponent<GwiOSBookingDescriptionBoxUnderTest> box = RenderBox();
        box.Find("input").Input("ed");

        box.Find("input").KeyDown("ArrowDown");

        Assert.Equal(
            box.Find(".gwios-description-box-option--highlighted").Id,
            box.Find("input").GetAttribute("aria-activedescendant"));
    }

    [Fact]
    public void StopsAtTheLastSuggestion()
    {
        IRenderedComponent<GwiOSBookingDescriptionBoxUnderTest> box = RenderBox();
        box.Find("input").Input("kred");

        box.Find("input").KeyDown("ArrowDown");
        box.Find("input").KeyDown("ArrowDown");
        box.Find("input").KeyDown("Enter");

        Assert.Equal([LoanRate], SelectedSuggestions);
    }

    [Fact]
    public void ClosesTheListWithoutPicking_WhenEscapeIsPressed()
    {
        IRenderedComponent<GwiOSBookingDescriptionBoxUnderTest> box = RenderBox();
        box.Find("input").Input("ed");

        box.Find("input").KeyDown("Escape");

        Assert.Empty(box.FindAll("[role='listbox']"));
        Assert.Empty(SelectedSuggestions);
    }

    [Fact]
    public void ReopensTheList_WhenArrowDownIsPressedAfterEscape()
    {
        IRenderedComponent<GwiOSBookingDescriptionBoxUnderTest> box = RenderBox();
        box.Find("input").Input("ed");
        box.Find("input").KeyDown("Escape");

        box.Find("input").KeyDown("ArrowDown");

        Assert.Equal(4, box.FindAll("[role='option']").Count);
    }
}
