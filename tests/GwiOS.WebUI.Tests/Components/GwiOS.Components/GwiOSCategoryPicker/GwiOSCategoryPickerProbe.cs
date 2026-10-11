using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOSCategoryPickerUnderTest = GwiOS.WebUI.Components.GwiOS.Components.GwiOSCategoryPicker;

namespace GwiOS.WebUI.Tests.Components.GwiOS.Components.GwiOSCategoryPicker;

/// <summary>
/// Renders a <c>GwiOSCategoryPicker</c> over a small set of categories of both types — "Haushalt › Lebensmittel" and
/// "Gehalt" are favorites, "Haushalt › Lebensmittel" and "Mobilität › Auto" are sub-categories — and records every
/// type and category it reports.
/// </summary>
public abstract class GwiOSCategoryPickerProbe : BunitContext
{
    protected GwiOSCategoryPickerProbe()
    {
        Groceries = new Category
        {
            Name = "Lebensmittel", Type = CategoryType.Expense, ParentId = Household.Id, IsFavorite = true
        };
        Car = new Category { Name = "Auto", Type = CategoryType.Expense, ParentId = Mobility.Id };
    }

    protected Category Household { get; } =
        new() { Name = "Haushalt", Type = CategoryType.Expense, Color = "#ffd056" };

    protected Category Groceries { get; }

    protected Category Mobility { get; } = new() { Name = "Mobilität", Type = CategoryType.Expense, Color = "#3399ff" };

    protected Category Car { get; }

    protected Category General { get; } = new() { Name = "Allgemein", Type = CategoryType.Expense, Color = "#ff4d6d" };

    protected Category Salary { get; } =
        new() { Name = "Gehalt", Type = CategoryType.Income, Color = "#00ffa3", IsFavorite = true };

    protected IReadOnlyList<Category> All
        => [Household, Groceries, Mobility, Car, General, Salary];

    /// <summary>
    /// What the picker reported, in order, e.g. "type Income" or "category {id}" (empty for none).
    /// </summary>
    protected List<string> Reports { get; } = [];

    protected IRenderedComponent<GwiOSCategoryPickerUnderTest> RenderPicker(
        IReadOnlyList<Category>? categories = null,
        Guid? selectedCategoryId = null,
        CategoryType type = CategoryType.Expense,
        string? errorMessage = null)
        => Render<GwiOSCategoryPickerUnderTest>(parameters => parameters
            .Add(component => component.Categories, categories ?? All)
            .Add(component => component.SelectedCategoryId, selectedCategoryId)
            .Add(component => component.SelectedCategoryIdChanged, (Guid? id) => Reports.Add($"category {id}"))
            .Add(component => component.Type, type)
            .Add(component => component.TypeChanged, (CategoryType newType) => Reports.Add($"type {newType}"))
            .Add(component => component.ErrorMessage, errorMessage));

    protected static void OpenTree(IRenderedComponent<GwiOSCategoryPickerUnderTest> picker)
        => picker.Find(".gwios-category-picker-open").Click();

    protected static void Filter(IRenderedComponent<GwiOSCategoryPickerUnderTest> picker, string text)
        => picker.Find("[role='dialog'] input").Input(text);

    protected static List<string> TreeItemNames(IRenderedComponent<GwiOSCategoryPickerUnderTest> picker)
        => [.. picker.FindAll(".gwios-category-picker-tree-item").Select(item => item.TextContent.Trim())];

    protected static void ClickTreeItem(IRenderedComponent<GwiOSCategoryPickerUnderTest> picker, string name)
        => picker.FindAll(".gwios-category-picker-tree-item").Single(item => item.TextContent.Trim() == name).Click();
}
