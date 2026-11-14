using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Models;
using GwiOSCategoryBookingsUnderTest = GwiOS.WebUI.Components.GwiOS.Components.GwiOSCategoryBookings;

namespace GwiOS.WebUI.Tests.Components.GwiOS.Components.GwiOSCategoryBookings;

/// <summary>
/// Covers which bookings <c>GwiOSCategoryBookings</c> lists and how, over "Haushalt" with "Lebensmittel" and, below
/// it, "Bio", plus "Mobilität".
/// </summary>
public sealed class RenderingTests : BunitContext
{
    private readonly Category _household = new() { Name = "Haushalt", Type = CategoryType.Expense, Color = "#ffd056" };
    private readonly Category _groceries;
    private readonly Category _organic;
    private readonly Category _mobility = new() { Name = "Mobilität", Type = CategoryType.Expense, Color = "#3399ff" };
    private readonly RecurringItem _rent;
    private readonly List<Booking> _bookings;

    public RenderingTests()
    {
        _groceries = new Category { Name = "Lebensmittel", Type = CategoryType.Expense, ParentId = _household.Id };
        _organic = new Category { Name = "Bio", Type = CategoryType.Expense, ParentId = _groceries.Id };
        _rent = new RecurringItem
        {
            Name = "Putzhilfe", Type = CategoryType.Expense, CategoryId = _household.Id, Amount = 80m,
            Interval = RecurringInterval.Monthly
        };
        _bookings =
        [
            CreateBooking(_household, 80m, null, new DateOnly(2026, 10, 9), _rent.Id),
            CreateBooking(_groceries, 47.83m, "Edeka", new DateOnly(2026, 10, 7)),
            CreateBooking(_organic, 2.5m, "Hofladen", new DateOnly(2026, 10, 5), status: BookingStatus.Pending),
            CreateBooking(_mobility, 60m, "Tanken", new DateOnly(2026, 10, 3))
        ];
    }

    [Fact]
    public void ListsOnlyTheBookingsOfTheCategoryItself_WithoutSubCategories()
    {
        IRenderedComponent<GwiOSCategoryBookingsUnderTest> list = RenderList(_household, includeSubCategories: false);

        Assert.Equal(["09.10. | ↻ Putzhilfe | 80,00 €"], RowTexts(list));
    }

    [Fact]
    public void ListsTheBookingsOfAllSubCategories_WithTheirCategoryPath()
    {
        IRenderedComponent<GwiOSCategoryBookingsUnderTest> list = RenderList(_household, includeSubCategories: true);

        Assert.Equal(
            [
                "09.10. | ↻ Putzhilfe | 80,00 €",
                "07.10. | Edeka Haushalt › Lebensmittel | 47,83 €",
                "05.10. | Hofladen Haushalt › Lebensmittel › Bio Ausstehend | 2,50 €"
            ],
            RowTexts(list));
    }

    [Fact]
    public void ShowsTheNumberAndTheSumOfTheBookings()
    {
        IRenderedComponent<GwiOSCategoryBookingsUnderTest> list = RenderList(_groceries, includeSubCategories: true);

        Assert.Equal(
            ["2 Buchungen", "50,33 €"],
            list.FindAll(".gwios-category-bookings-total span").Select(span => span.TextContent));
    }

    [Fact]
    public void SaysSo_WhenTheCategoryHasNoBookings()
    {
        IRenderedComponent<GwiOSCategoryBookingsUnderTest> list = RenderList(_groceries, includeSubCategories: false, bookings: []);

        Assert.Equal("Keine Buchungen.", list.Find(".gwios-category-bookings-empty").TextContent);
        Assert.Empty(list.FindAll(".gwios-category-bookings-total"));
    }

    private IRenderedComponent<GwiOSCategoryBookingsUnderTest> RenderList(
        Category category, bool includeSubCategories, IReadOnlyList<Booking>? bookings = null)
        => Render<GwiOSCategoryBookingsUnderTest>(parameters => parameters
            .Add(component => component.CategoryId, category.Id)
            .Add(component => component.IncludeSubCategories, includeSubCategories)
            .Add(component => component.Bookings, bookings ?? _bookings)
            .Add(component => component.Categories, [_household, _groceries, _organic, _mobility])
            .Add(component => component.RecurringItems, [_rent]));

    // The cells joined by " | "; within the text cell, its parts (description, labels, badge) joined by blanks.
    private static List<string> RowTexts(IRenderedComponent<GwiOSCategoryBookingsUnderTest> list)
        => [.. list.FindAll(".gwios-category-bookings-row").Select(row => string.Join(
            " | ",
            row.Children.Select(cell => cell.Children.Length == 0
                ? Normalize(cell.TextContent)
                : string.Join(' ', cell.Children.Select(part => Normalize(part.TextContent))))))];

    private static string Normalize(string text)
        => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static Booking CreateBooking(
        Category category,
        decimal amount,
        string? description,
        DateOnly date,
        Guid? recurringItemId = null,
        BookingStatus status = BookingStatus.Booked)
        => new()
        {
            Date = date, Type = category.Type, CategoryId = category.Id, Description = description, Amount = amount,
            Status = status, RecurringItemId = recurringItemId
        };
}
