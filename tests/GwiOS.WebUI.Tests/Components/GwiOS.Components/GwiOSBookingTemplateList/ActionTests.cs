using AngleSharp.Dom;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Models;
using GwiOSBookingTemplateListUnderTest = GwiOS.WebUI.Components.GwiOS.Components.GwiOSBookingTemplateList;

namespace GwiOS.WebUI.Tests.Components.GwiOS.Components.GwiOSBookingTemplateList;

/// <summary>
/// Covers how <c>GwiOSBookingTemplateList</c> reports moving, renaming and deleting a favorite.
/// </summary>
public sealed class ActionTests : GwiOSBookingTemplateListProbe
{
    [Fact]
    public void ReportsTheTemplateToMoveUp()
    {
        IRenderedComponent<GwiOSBookingTemplateListUnderTest> list = RenderList();

        GetRow(list, "Aral").QuerySelector("button[title='Nach oben']")!.Click();

        Assert.Equal(["up Aral"], Reports);
    }

    [Fact]
    public void ReportsTheTemplateToMoveDown()
    {
        IRenderedComponent<GwiOSBookingTemplateListUnderTest> list = RenderList();

        GetRow(list, "Edeka").QuerySelector("button[title='Nach unten']")!.Click();

        Assert.Equal(["down Edeka"], Reports);
    }

    [Fact]
    public void ReportsARenamedCopy_WhenANewNameIsSavedWithEnter()
    {
        IRenderedComponent<GwiOSBookingTemplateListUnderTest> list = RenderList();
        BookingTemplate baecker = Templates[1];
        GetRow(list, "Bäcker Müller").QuerySelector("button[title='Umbenennen']")!.Click();

        list.Find(".gwios-booking-template-rename").Input("  Bäcker Müller (Ecke) ");
        list.Find(".gwios-booking-template-rename").KeyDown("Enter");

        BookingTemplate renamed = Assert.Single(RenamedTemplates);
        Assert.Equal(baecker.Id, renamed.Id);
        Assert.Equal("Bäcker Müller (Ecke)", renamed.Description);
        Assert.Equal(baecker.CategoryId, renamed.CategoryId);
        Assert.Equal("Bäcker Müller", baecker.Description);
        Assert.Empty(list.FindAll(".gwios-booking-template-rename"));
    }

    [Fact]
    public void PrefillsTheRenameFieldWithTheCurrentName()
    {
        IRenderedComponent<GwiOSBookingTemplateListUnderTest> list = RenderList();

        GetRow(list, "Edeka").QuerySelector("button[title='Umbenennen']")!.Click();

        Assert.Equal("Edeka", list.Find(".gwios-booking-template-rename").GetAttribute("value"));
    }

    [Fact]
    public void ReportsNothing_WhenRenamingIsCancelledWithEscape()
    {
        IRenderedComponent<GwiOSBookingTemplateListUnderTest> list = RenderList();
        GetRow(list, "Edeka").QuerySelector("button[title='Umbenennen']")!.Click();
        list.Find(".gwios-booking-template-rename").Input("Rewe");

        list.Find(".gwios-booking-template-rename").KeyDown("Escape");

        Assert.Empty(RenamedTemplates);
        Assert.Empty(list.FindAll(".gwios-booking-template-rename"));
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("Edeka")]
    public void ReportsNothing_WhenTheNewNameIsEmptyOrUnchanged(string newName)
    {
        IRenderedComponent<GwiOSBookingTemplateListUnderTest> list = RenderList();
        GetRow(list, "Edeka").QuerySelector("button[title='Umbenennen']")!.Click();
        list.Find(".gwios-booking-template-rename").Input(newName);

        list.FindAll("button").Single(button => button.TextContent.Trim() == "Speichern").Click();

        Assert.Empty(RenamedTemplates);
    }

    [Fact]
    public void AsksForConfirmationBeforeReportingTheTemplateToDelete()
    {
        IRenderedComponent<GwiOSBookingTemplateListUnderTest> list = RenderList();

        GetRow(list, "Aral").QuerySelector("button[title='Löschen']")!.Click();

        Assert.Empty(Reports);
        IElement confirmButton = GetRow(list, "Aral").QuerySelector(".gwios-booking-template-actions button:last-child")!;
        Assert.Equal("Sicher?", confirmButton.TextContent.Trim());

        confirmButton.Click();

        Assert.Equal(["delete Aral"], Reports);
    }

    private static IElement GetRow(IRenderedComponent<GwiOSBookingTemplateListUnderTest> list, string description)
        => list.FindAll(".gwios-booking-template-row")
            .Single(row => row.QuerySelector(".gwios-booking-template-name")?.TextContent == description);
}
