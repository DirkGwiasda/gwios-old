using AngleSharp.Dom;
using GwiOSBookingTemplateListUnderTest = GwiOS.WebUI.Components.GwiOS.Components.GwiOSBookingTemplateList;

namespace GwiOS.WebUI.Tests.Components.GwiOS.Components.GwiOSBookingTemplateList;

/// <summary>
/// Covers how <c>GwiOSBookingTemplateList</c> shows the favorites.
/// </summary>
public sealed class RenderingTests : GwiOSBookingTemplateListProbe
{
    [Fact]
    public void ShowsEachTemplateWithItsCategoryInTheGivenOrder()
    {
        IRenderedComponent<GwiOSBookingTemplateListUnderTest> list = RenderList();

        Assert.Equal(
            ["Edeka", "Bäcker Müller", "Aral"],
            list.FindAll(".gwios-booking-template-name").Select(name => name.TextContent));
        Assert.All(
            list.FindAll(".gwios-category-label-path"),
            path => Assert.Equal("Lebensmittel", path.TextContent));
    }

    [Fact]
    public void DisablesUpForTheFirstAndDownForTheLastTemplate()
    {
        IRenderedComponent<GwiOSBookingTemplateListUnderTest> list = RenderList();

        IReadOnlyList<IElement> rows = list.FindAll(".gwios-booking-template-row");
        Assert.True(rows[0].QuerySelector("button[title='Nach oben']")!.HasAttribute("disabled"));
        Assert.False(rows[0].QuerySelector("button[title='Nach unten']")!.HasAttribute("disabled"));
        Assert.False(rows[2].QuerySelector("button[title='Nach oben']")!.HasAttribute("disabled"));
        Assert.True(rows[2].QuerySelector("button[title='Nach unten']")!.HasAttribute("disabled"));
    }

    [Fact]
    public void ExplainsHowToPinFavorites_WhenThereAreNone()
    {
        IRenderedComponent<GwiOSBookingTemplateListUnderTest> list = RenderList([]);

        Assert.Empty(list.FindAll(".gwios-booking-template-row"));
        Assert.Contains("Noch keine Favoriten", list.Find(".gwios-empty").TextContent);
    }
}
