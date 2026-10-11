using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOSCategoryPickerUnderTest = GwiOS.WebUI.Components.GwiOS.Components.GwiOSCategoryPicker;

namespace GwiOS.WebUI.Tests.Components.GwiOS.Components.GwiOSCategoryPicker;

/// <summary>
/// Covers what <c>GwiOSCategoryPicker</c> shows before the user does anything.
/// </summary>
public sealed class RenderingTests : GwiOSCategoryPickerProbe
{
    [Fact]
    public void ShowsTheFavoriteCategoriesOfBothTypesAsChips()
    {
        IRenderedComponent<GwiOSCategoryPickerUnderTest> picker = RenderPicker();

        Assert.Equal(
            ["Lebensmittel", "Gehalt"],
            picker.FindAll(".gwios-chip").Select(chip => chip.TextContent.Trim()));
        Assert.Equal("Haushalt › Lebensmittel", picker.FindAll(".gwios-chip")[0].GetAttribute("title"));
    }

    [Fact]
    public void ShowsAHowToNoteInsteadOfChips_WhenNoCategoryIsAFavorite()
    {
        Category noFavorite = new() { Name = "Haushalt", Type = CategoryType.Expense, Color = "#ffd056" };

        IRenderedComponent<GwiOSCategoryPickerUnderTest> picker = RenderPicker(categories: [noFavorite]);

        Assert.Empty(picker.FindAll(".gwios-chip"));
        Assert.Contains("mit ☆ als Favorit markieren", picker.Find(".gwios-category-picker-note").TextContent);
    }

    [Fact]
    public void MarksTheChipOfTheSelectedCategoryAndShowsItsPath()
    {
        IRenderedComponent<GwiOSCategoryPickerUnderTest> picker = RenderPicker(selectedCategoryId: Groceries.Id);

        Assert.Equal("Lebensmittel", picker.Find(".gwios-chip--selected").TextContent.Trim());
        Assert.Equal(
            "Haushalt › Lebensmittel",
            picker.Find(".gwios-category-picker-selected .gwios-category-label-path").TextContent);
    }

    [Theory]
    [InlineData(CategoryType.Expense, "Ausgabe")]
    [InlineData(CategoryType.Income, "Einnahme")]
    public void MarksTheCurrentType(CategoryType type, string expectedLabel)
    {
        IRenderedComponent<GwiOSCategoryPickerUnderTest> picker = RenderPicker(type: type);

        Assert.Equal(
            expectedLabel,
            picker.FindAll(".gwios-category-picker-type").Single(button => button.GetAttribute("aria-pressed") == "true")
                .TextContent.Trim());
    }

    [Fact]
    public void ShowsTheError_WhenOneIsGiven()
    {
        IRenderedComponent<GwiOSCategoryPickerUnderTest> picker =
            RenderPicker(errorMessage: "Bitte eine Kategorie wählen.");

        Assert.Equal("Bitte eine Kategorie wählen.", picker.Find(".gwios-category-picker-error").TextContent);
    }

    [Fact]
    public void ShowsTheSearchButtonButKeepsTheTreeDialogClosedAtFirst()
    {
        IRenderedComponent<GwiOSCategoryPickerUnderTest> picker = RenderPicker();

        Assert.Equal("🔍 Kategorie suchen", picker.Find(".gwios-category-picker-open").TextContent.Trim());
        Assert.Empty(picker.FindAll("[role='dialog']"));
    }
}
