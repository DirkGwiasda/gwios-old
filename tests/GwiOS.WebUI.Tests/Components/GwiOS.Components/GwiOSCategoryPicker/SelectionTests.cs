using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOSCategoryPickerUnderTest = GwiOS.WebUI.Components.GwiOS.Components.GwiOSCategoryPicker;

namespace GwiOS.WebUI.Tests.Components.GwiOS.Components.GwiOSCategoryPicker;

/// <summary>
/// Covers how <c>GwiOSCategoryPicker</c> reports picked categories and type changes.
/// </summary>
public sealed class SelectionTests : GwiOSCategoryPickerProbe
{
    [Fact]
    public void ReportsTheCategory_WhenAFavoriteChipOfTheSameTypeIsClicked()
    {
        IRenderedComponent<GwiOSCategoryPickerUnderTest> picker = RenderPicker();

        ClickChip(picker, "Lebensmittel");

        Assert.Equal([$"category {Groceries.Id}"], Reports);
    }

    [Fact]
    public void ReportsTheNewTypeBeforeTheCategory_WhenACategoryOfTheOtherTypeIsPicked()
    {
        IRenderedComponent<GwiOSCategoryPickerUnderTest> picker = RenderPicker();

        ClickChip(picker, "Gehalt");

        Assert.Equal(["type Income", $"category {Salary.Id}"], Reports);
    }

    [Fact]
    public void ClearsTheCategory_WhenTheTypeIsSwitchedAwayFromIt()
    {
        IRenderedComponent<GwiOSCategoryPickerUnderTest> picker = RenderPicker(selectedCategoryId: Groceries.Id);

        ClickType(picker, "Einnahme");

        Assert.Equal(["type Income", "category "], Reports);
    }

    [Fact]
    public void KeepsTheCategory_WhenTheTypeIsSwitchedWithoutACategory()
    {
        IRenderedComponent<GwiOSCategoryPickerUnderTest> picker = RenderPicker();

        ClickType(picker, "Einnahme");

        Assert.Equal(["type Income"], Reports);
    }

    [Fact]
    public void ReportsNothing_WhenTheCurrentTypeIsClicked()
    {
        IRenderedComponent<GwiOSCategoryPickerUnderTest> picker = RenderPicker(type: CategoryType.Expense);

        ClickType(picker, "Ausgabe");

        Assert.Empty(Reports);
    }

    [Fact]
    public void ReportsTheClickedCategoryAndClosesTheTreeDialog_WhenATreeItemIsClicked()
    {
        IRenderedComponent<GwiOSCategoryPickerUnderTest> picker = RenderPicker();
        OpenTree(picker);

        ClickTreeItem(picker, "Mobilität");

        Assert.Equal([$"category {Mobility.Id}"], Reports);
        Assert.Empty(picker.FindAll("[role='dialog']"));
    }

    [Fact]
    public void ReportsTheNewTypeBeforeTheCategory_WhenATreeItemOfTheOtherTypeIsClicked()
    {
        IRenderedComponent<GwiOSCategoryPickerUnderTest> picker = RenderPicker();
        OpenTree(picker);

        ClickTreeItem(picker, "Gehalt");

        Assert.Equal(["type Income", $"category {Salary.Id}"], Reports);
    }

    private static void ClickChip(IRenderedComponent<GwiOSCategoryPickerUnderTest> picker, string name)
        => picker.FindAll(".gwios-chip").Single(chip => chip.TextContent.Trim() == name).Click();

    private static void ClickType(IRenderedComponent<GwiOSCategoryPickerUnderTest> picker, string label)
        => picker.FindAll(".gwios-category-picker-type").Single(button => button.TextContent.Trim() == label).Click();
}
