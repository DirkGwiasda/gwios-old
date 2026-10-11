using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts.Models;
using GwiOSBookingDescriptionBoxUnderTest = GwiOS.WebUI.Components.GwiOS.Components.GwiOSBookingDescriptionBox;

namespace GwiOS.WebUI.Tests.Components.GwiOS.Components.GwiOSBookingDescriptionBox;

/// <summary>
/// Covers which suggestions <c>GwiOSBookingDescriptionBox</c> lists for a typed text, and in which order.
/// </summary>
public sealed class MatchingTests : GwiOSBookingDescriptionBoxProbe
{
    [Fact]
    public void ListsNothing_BeforeTwoCharactersAreTyped()
    {
        IRenderedComponent<GwiOSBookingDescriptionBoxUnderTest> box = RenderBox();

        box.Find("input").Input("e");

        Assert.Empty(box.FindAll("[role='listbox']"));
        Assert.Equal("false", box.Find("input").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void ListsWordStartsFirstThenOtherMatches_EachByUsageThenRecency_IgnoringCase()
    {
        IRenderedComponent<GwiOSBookingDescriptionBoxUnderTest> box = RenderBox();

        box.Find("input").Input("ED");

        Assert.Equal(
            [
                "Edeka | Haushalt › Lebensmittel",
                "Bäcker Edeltraud | Haushalt › Lebensmittel",
                "Edeka | Haushalt › Drogerie",
                "Kreditrate | Mobilität"
            ],
            OptionTexts(box));
        Assert.Equal("true", box.Find("input").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void ListsAtMostEightSuggestions()
    {
        List<BookingSuggestion> manySuggestions = [.. Enumerable.Range(1, 10).Select(number => new BookingSuggestion
        {
            Description = $"Edeka {number}", CategoryId = Groceries.Id, UsageCount = number,
            LastUsedOn = new DateOnly(2026, 10, 1)
        })];
        IRenderedComponent<GwiOSBookingDescriptionBoxUnderTest> box = RenderBox(manySuggestions);

        box.Find("input").Input("ed");

        Assert.Equal(8, box.FindAll("[role='option']").Count);
    }

    [Fact]
    public void LeavesOutSuggestionsOfUnknownCategories()
    {
        BookingSuggestion orphan = new()
        {
            Description = "Edeka", CategoryId = Guid.CreateVersion7(), UsageCount = 9, LastUsedOn = new DateOnly(2026, 10, 1)
        };
        IRenderedComponent<GwiOSBookingDescriptionBoxUnderTest> box = RenderBox([orphan, EdekaGroceries]);

        box.Find("input").Input("ed");

        Assert.Equal(["Edeka | Haushalt › Lebensmittel"], OptionTexts(box));
    }

    [Fact]
    public void ListsNothing_WhenNothingMatches()
    {
        IRenderedComponent<GwiOSBookingDescriptionBoxUnderTest> box = RenderBox();

        box.Find("input").Input("xy");

        Assert.Empty(box.FindAll("[role='listbox']"));
    }
}
