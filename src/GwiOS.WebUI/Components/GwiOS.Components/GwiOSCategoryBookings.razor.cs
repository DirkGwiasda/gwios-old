using System.Globalization;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Models;
using Microsoft.AspNetCore.Components;

namespace GwiOS.WebUI.Components.GwiOS.Components;

/// <summary>
/// Lists the bookings of one category, e.g. those behind a line of the month overview: date, description (or the
/// recurring item it was generated from), amount and whether it is still pending, then the number of bookings and
/// their sum. With sub-categories included, a booking of a sub-category also shows its category path. The list is
/// read-only.
/// </summary>
public partial class GwiOSCategoryBookings
{
    // Categories nest at most 5 levels deep; the limit only protects against a cycle in inconsistent data.
    private const int MaxDepth = 8;

    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

    private List<Booking> _bookings = [];

    /// <summary>
    /// The ID of the category whose bookings are listed.
    /// </summary>
    [Parameter, EditorRequired]
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Whether the bookings of all sub-categories, at any depth, are listed as well.
    /// </summary>
    [Parameter]
    public bool IncludeSubCategories { get; set; }

    /// <summary>
    /// The bookings to pick from, e.g. those of the shown month; listed in the given order.
    /// </summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<Booking> Bookings { get; set; } = [];

    /// <summary>
    /// All categories, to find the sub-categories and to show category paths.
    /// </summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<Category> Categories { get; set; } = [];

    /// <summary>
    /// All recurring items, to label bookings generated from one.
    /// </summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<RecurringItem> RecurringItems { get; set; } = [];

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        HashSet<Guid> categoryIds = IncludeSubCategories ? CollectWithSubCategories(CategoryId) : [CategoryId];
        _bookings = [.. Bookings.Where(booking => categoryIds.Contains(booking.CategoryId))];
    }

    private HashSet<Guid> CollectWithSubCategories(Guid rootId)
    {
        ILookup<Guid?, Guid> childIdsByParent =
            Categories.ToLookup(category => category.ParentId, category => category.Id);
        HashSet<Guid> categoryIds = [rootId];
        List<Guid> currentLevel = [rootId];
        for (int depth = 0; (depth < MaxDepth) && (currentLevel.Count > 0); depth++)
        {
            currentLevel = [.. currentLevel.SelectMany(id => childIdsByParent[id]).Where(categoryIds.Add)];
        }

        return categoryIds;
    }

    private static string FormatAmount(decimal value)
        => value.ToString("C", GermanCulture);
}
