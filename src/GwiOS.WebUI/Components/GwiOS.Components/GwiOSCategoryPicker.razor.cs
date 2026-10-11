using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using Microsoft.AspNetCore.Components;

namespace GwiOS.WebUI.Components.GwiOS.Components;

/// <summary>
/// Picks the category of a booking without a long drop-down: the favorite categories as chips, and a button that
/// opens the whole tree of both types in a dialog. A text box above the tree filters it by the start of the category
/// names, ignoring case: "a" finds "Auto" and "Allgemein", "au" only "Auto". The tree then shows only the matching
/// categories, together with their parent categories, which stay visible for orientation but cannot be picked.
/// Picking a category also sets the booking type to the type of the category; switching the type clears a category
/// of the other type.
/// </summary>
public partial class GwiOSCategoryPicker
{
    private const string FallbackColor = "#7c93b3";

    // Categories nest at most 5 levels deep; the limit only protects against a cycle in inconsistent data.
    private const int MaxDepth = 8;

    private static readonly (CategoryType Type, string Title)[] TreeColumns =
        [(CategoryType.Expense, "Ausgaben"), (CategoryType.Income, "Einnahmen")];

    private Dictionary<Guid, Category> _categoriesById = [];
    private string _filterText = string.Empty;
    private bool _isTreeOpen;

    /// <summary>
    /// All categories of both types, flat; paths and the tree are built from their <see cref="Category.ParentId"/>.
    /// </summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<Category> Categories { get; set; } = [];

    /// <summary>
    /// The ID of the selected category, or <c>null</c> if none is selected.
    /// </summary>
    [Parameter]
    public Guid? SelectedCategoryId { get; set; }

    /// <summary>
    /// Raised with the ID of the category the user picks, or with <c>null</c> when switching the type cleared it.
    /// </summary>
    [Parameter]
    public EventCallback<Guid?> SelectedCategoryIdChanged { get; set; }

    /// <summary>
    /// The type of the booking.
    /// </summary>
    [Parameter]
    public CategoryType Type { get; set; } = CategoryType.Expense;

    /// <summary>
    /// Raised with the new type when the user switches it or picks a category of the other type. Raised before
    /// <see cref="SelectedCategoryIdChanged"/>.
    /// </summary>
    [Parameter]
    public EventCallback<CategoryType> TypeChanged { get; set; }

    /// <summary>
    /// Error shown below the picker, or <c>null</c> while the selection is valid.
    /// </summary>
    [Parameter]
    public string? ErrorMessage { get; set; }

    // Expenses first, since most bookings are expenses.
    private List<Category> FavoriteCategories
        => [.. Categories
            .Where(category => category.IsFavorite)
            .OrderBy(category => category.Type != CategoryType.Expense)
            .ThenBy(category => BuildPath(category.Id), StringComparer.CurrentCultureIgnoreCase)];

    // Leading blanks are ignored, trailing ones are part of the prefix ("bank " does not find "Bankgebühren").
    private string FilterPrefix
        => _filterText.TrimStart();

    // The tree columns that have something to show; without a filter both.
    private List<(CategoryType Type, string Title, List<(Category Category, int Depth)> Items)> VisibleTreeColumns
        => [.. TreeColumns
            .Select(column => (column.Type, column.Title, Items: FlattenTree(column.Type)))
            .Where(column => column.Items.Count > 0)];

    /// <inheritdoc />
    protected override void OnParametersSet()
        => _categoriesById = Categories.ToDictionary(category => category.Id);

    private void OnFilterChanged(string filterText)
        => _filterText = filterText;

    private void OpenTree()
    {
        _filterText = string.Empty;
        _isTreeOpen = true;
    }

    private void CloseTree()
        => _isTreeOpen = false;

    private bool MatchesFilter(Category category)
        => category.Name.StartsWith(FilterPrefix, StringComparison.CurrentCultureIgnoreCase);

    private async Task SelectCategoryAsync(Category category)
    {
        _isTreeOpen = false;
        if (category.Type != Type)
        {
            Type = category.Type;
            await TypeChanged.InvokeAsync(category.Type);
        }

        SelectedCategoryId = category.Id;
        await SelectedCategoryIdChanged.InvokeAsync(category.Id);
    }

    private async Task SelectTypeAsync(CategoryType type)
    {
        if (type == Type)
        {
            return;
        }

        Type = type;
        await TypeChanged.InvokeAsync(type);
        if ((SelectedCategoryId is Guid categoryId)
            && _categoriesById.TryGetValue(categoryId, out Category? category)
            && (category.Type != type))
        {
            SelectedCategoryId = null;
            await SelectedCategoryIdChanged.InvokeAsync(null);
        }
    }

    // Root first, e.g. "Haushalt › Lebensmittel".
    private string BuildPath(Guid categoryId)
        => string.Join(" › ", CollectLineage(categoryId).Select(category => category.Name));

    // Only root categories carry a color; a sub-category shows the color of its root.
    private string ResolveColor(Guid categoryId)
        => CollectLineage(categoryId).FirstOrDefault()?.Color ?? FallbackColor;

    // The category and its ancestors, from the root category down to the category itself.
    private List<Category> CollectLineage(Guid categoryId)
    {
        List<Category> lineage = [];
        Guid? currentId = categoryId;
        while ((currentId is Guid id) && _categoriesById.TryGetValue(id, out Category? category) && (lineage.Count < MaxDepth))
        {
            lineage.Insert(0, category);
            currentId = category.ParentId;
        }

        return lineage;
    }

    // The categories of one type in tree order (each category followed by its children), with their depth: the ones
    // matching the filter and their ancestors.
    private List<(Category Category, int Depth)> FlattenTree(CategoryType type)
    {
        HashSet<Guid> visibleIds = [.. Categories
            .Where(category => (category.Type == type) && MatchesFilter(category))
            .SelectMany(category => CollectLineage(category.Id))
            .Select(category => category.Id)];
        List<(Category Category, int Depth)> items = [];
        AppendChildren(type, null, 0, visibleIds, items);
        return items;
    }

    private void AppendChildren(
        CategoryType type, Guid? parentId, int depth, HashSet<Guid> visibleIds, List<(Category Category, int Depth)> items)
    {
        if (depth >= MaxDepth)
        {
            return;
        }

        IEnumerable<Category> children = Categories
            .Where(category => (category.Type == type) && (category.ParentId == parentId) && visibleIds.Contains(category.Id))
            .OrderBy(category => category.Name, StringComparer.CurrentCultureIgnoreCase);
        foreach (Category child in children)
        {
            items.Add((child, depth));
            AppendChildren(type, child.Id, depth + 1, visibleIds, items);
        }
    }
}
