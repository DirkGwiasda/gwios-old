using GwiOSCategoryPickerUnderTest = GwiOS.WebUI.Components.GwiOS.Components.GwiOSCategoryPicker;

namespace GwiOS.WebUI.Tests.Components.GwiOS.Components.GwiOSCategoryPicker;

/// <summary>
/// Covers the dialog with the category tree that the search button of <c>GwiOSCategoryPicker</c> opens, and how its
/// filter narrows the tree.
/// </summary>
public sealed class TreeDialogTests : GwiOSCategoryPickerProbe
{
    [Fact]
    public void ShowsTheWholeTreeOfBothTypesWithAnEmptyFocusedFilter_WhenOpened()
    {
        IRenderedComponent<GwiOSCategoryPickerUnderTest> picker = RenderPicker();

        OpenTree(picker);

        Assert.Equal("Kategorie wählen", picker.Find(".gwios-dialog-title").TextContent);
        Assert.Equal(string.Empty, picker.Find("[role='dialog'] input").GetAttribute("value"));
        Assert.Equal(
            ["Ausgaben", "Einnahmen"],
            picker.FindAll(".gwios-category-picker-tree-title").Select(title => title.TextContent));
        Assert.Equal(["Allgemein", "Haushalt", "Lebensmittel", "Mobilität", "Auto", "Gehalt"], TreeItemNames(picker));
        Assert.All(picker.FindAll(".gwios-category-picker-tree-item"), item => Assert.False(item.HasAttribute("disabled")));
        JSInterop.VerifyFocusAsyncInvoke(calledTimes: 2);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("A")]
    public void FindsAllCategoriesStartingWithTheText_IgnoringCase(string filter)
    {
        IRenderedComponent<GwiOSCategoryPickerUnderTest> picker = RenderPicker();
        OpenTree(picker);

        Filter(picker, filter);

        Assert.Equal(["Allgemein", "Mobilität", "Auto"], TreeItemNames(picker));
    }

    [Fact]
    public void NarrowsTheTree_WhenTheTextGetsLonger()
    {
        IRenderedComponent<GwiOSCategoryPickerUnderTest> picker = RenderPicker();
        OpenTree(picker);

        Filter(picker, "au");

        Assert.Equal(["Mobilität", "Auto"], TreeItemNames(picker));
    }

    [Fact]
    public void MatchesOnlyTheStartOfTheName()
    {
        IRenderedComponent<GwiOSCategoryPickerUnderTest> picker = RenderPicker();
        OpenTree(picker);

        Filter(picker, "mittel");

        Assert.Empty(picker.FindAll(".gwios-category-picker-tree-item"));
    }

    [Fact]
    public void ShowsTheParentOfAMatchOnlyForOrientation()
    {
        IRenderedComponent<GwiOSCategoryPickerUnderTest> picker = RenderPicker();
        OpenTree(picker);

        Filter(picker, "au");

        Assert.True(picker.FindAll(".gwios-category-picker-tree-item")[0].HasAttribute("disabled"));
        Assert.False(picker.FindAll(".gwios-category-picker-tree-item")[1].HasAttribute("disabled"));
    }

    [Fact]
    public void HidesTheColumnOfATypeWithoutMatches()
    {
        IRenderedComponent<GwiOSCategoryPickerUnderTest> picker = RenderPicker();
        OpenTree(picker);

        Filter(picker, "geh");

        Assert.Equal(["Einnahmen"], picker.FindAll(".gwios-category-picker-tree-title").Select(title => title.TextContent));
        Assert.Equal(["Gehalt"], TreeItemNames(picker));
    }

    [Fact]
    public void SaysSo_WhenNoCategoryStartsWithTheText()
    {
        IRenderedComponent<GwiOSCategoryPickerUnderTest> picker = RenderPicker();
        OpenTree(picker);

        Filter(picker, "  xyz");

        Assert.Equal("Keine Kategorie beginnt mit „xyz“.", picker.Find(".gwios-category-picker-none").TextContent.Trim());
    }

    [Fact]
    public void ClearsTheFilter_WhenOpenedAgain()
    {
        IRenderedComponent<GwiOSCategoryPickerUnderTest> picker = RenderPicker();
        OpenTree(picker);
        Filter(picker, "geh");
        picker.Find(".gwios-dialog-close").Click();

        OpenTree(picker);

        Assert.Equal(6, picker.FindAll(".gwios-category-picker-tree-item").Count);
    }

    [Fact]
    public void ClosesWithoutReporting_WhenTheDialogIsClosed()
    {
        IRenderedComponent<GwiOSCategoryPickerUnderTest> picker = RenderPicker();
        OpenTree(picker);

        picker.Find(".gwios-dialog-close").Click();

        Assert.Empty(picker.FindAll("[role='dialog']"));
        Assert.Empty(Reports);
    }
}
