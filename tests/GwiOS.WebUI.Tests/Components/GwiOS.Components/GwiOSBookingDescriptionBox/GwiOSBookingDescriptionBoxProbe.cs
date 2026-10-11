using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOSBookingDescriptionBoxUnderTest = GwiOS.WebUI.Components.GwiOS.Components.GwiOSBookingDescriptionBox;

namespace GwiOS.WebUI.Tests.Components.GwiOS.Components.GwiOSBookingDescriptionBox;

/// <summary>
/// Renders a <c>GwiOSBookingDescriptionBox</c> with a few suggestions — "Edeka" twice (Lebensmittel used 5 times,
/// Drogerie once), "Kreditrate" (3 times) and "Bäcker Edeltraud" (once) — and records every reported text and
/// picked suggestion.
/// </summary>
public abstract class GwiOSBookingDescriptionBoxProbe : BunitContext
{
    protected GwiOSBookingDescriptionBoxProbe()
    {
        Groceries = new Category { Name = "Lebensmittel", Type = CategoryType.Expense, ParentId = Household.Id };
        Drugstore = new Category { Name = "Drogerie", Type = CategoryType.Expense, ParentId = Household.Id };
        EdekaGroceries = CreateSuggestion("Edeka", Groceries, 5, new DateOnly(2026, 10, 1));
        EdekaDrugstore = CreateSuggestion("Edeka", Drugstore, 1, new DateOnly(2026, 9, 1));
        LoanRate = CreateSuggestion("Kreditrate", Mobility, 3, new DateOnly(2026, 8, 1));
        Bakery = CreateSuggestion("Bäcker Edeltraud", Groceries, 1, new DateOnly(2026, 10, 2));
    }

    protected Category Household { get; } = new() { Name = "Haushalt", Type = CategoryType.Expense, Color = "#ffd056" };

    protected Category Groceries { get; }

    protected Category Drugstore { get; }

    protected Category Mobility { get; } = new() { Name = "Mobilität", Type = CategoryType.Expense, Color = "#3399ff" };

    protected BookingSuggestion EdekaGroceries { get; }

    protected BookingSuggestion EdekaDrugstore { get; }

    protected BookingSuggestion LoanRate { get; }

    protected BookingSuggestion Bakery { get; }

    /// <summary>
    /// The texts reported through <c>ValueChanged</c>, in order.
    /// </summary>
    protected List<string> ReportedValues { get; } = [];

    /// <summary>
    /// The suggestions reported through <c>OnSuggestionSelected</c>, in order.
    /// </summary>
    protected List<BookingSuggestion> SelectedSuggestions { get; } = [];

    protected IRenderedComponent<GwiOSBookingDescriptionBoxUnderTest> RenderBox(
        IReadOnlyList<BookingSuggestion>? suggestions = null)
        => Render<GwiOSBookingDescriptionBoxUnderTest>(parameters => parameters
            .Add(component => component.InputId, "description")
            .Add(component => component.ValueChanged, (string value) => ReportedValues.Add(value))
            .Add(component => component.Suggestions, suggestions ?? [EdekaGroceries, EdekaDrugstore, LoanRate, Bakery])
            .Add(component => component.Categories, [Household, Groceries, Drugstore, Mobility])
            .Add(component => component.OnSuggestionSelected, (BookingSuggestion suggestion) => SelectedSuggestions.Add(suggestion)));

    protected static List<string> OptionTexts(IRenderedComponent<GwiOSBookingDescriptionBoxUnderTest> box)
        => [.. box.FindAll("[role='option']").Select(option =>
            $"{option.QuerySelector(".gwios-description-box-text")!.TextContent} | "
            + option.QuerySelector(".gwios-category-label-path")!.TextContent)];

    private static BookingSuggestion CreateSuggestion(string description, Category category, int usageCount, DateOnly lastUsedOn)
        => new() { Description = description, CategoryId = category.Id, UsageCount = usageCount, LastUsedOn = lastUsedOn };
}
