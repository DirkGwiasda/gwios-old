using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Models;
using GwiOSBookingTemplateChipsUnderTest = GwiOS.WebUI.Components.GwiOS.Components.GwiOSBookingTemplateChips;

namespace GwiOS.WebUI.Tests.Components.GwiOS.Components.GwiOSBookingTemplateChips;

/// <summary>
/// Covers how <c>GwiOSBookingTemplateChips</c> shows the favorites and reports the picked one.
/// </summary>
public sealed class RenderingTests : BunitContext
{
    private readonly BookingTemplate _edeka = new() { Description = "Edeka", CategoryId = Guid.CreateVersion7() };
    private readonly BookingTemplate _baecker = new() { Description = "Bäcker", CategoryId = Guid.CreateVersion7() };
    private readonly List<BookingTemplate> _picked = [];

    [Fact]
    public void ShowsOneStarredChipPerTemplateInTheGivenOrder()
    {
        IRenderedComponent<GwiOSBookingTemplateChipsUnderTest> chips = RenderChips([_edeka, _baecker], null);

        Assert.Equal(["★Edeka", "★Bäcker"], chips.FindAll(".gwios-chip").Select(chip => chip.TextContent.Trim()));
    }

    [Fact]
    public void MarksTheActiveTemplate()
    {
        IRenderedComponent<GwiOSBookingTemplateChipsUnderTest> chips = RenderChips([_edeka, _baecker], _baecker.Id);

        Assert.Equal("★Bäcker", chips.Find(".gwios-chip--selected").TextContent.Trim());
    }

    [Fact]
    public void ReportsTheTemplate_WhenItsChipIsClicked()
    {
        IRenderedComponent<GwiOSBookingTemplateChipsUnderTest> chips = RenderChips([_edeka, _baecker], null);

        chips.FindAll(".gwios-chip")[1].Click();

        Assert.Same(_baecker, Assert.Single(_picked));
    }

    [Fact]
    public void ExplainsHowToPinFavorites_WhenThereAreNone()
    {
        IRenderedComponent<GwiOSBookingTemplateChipsUnderTest> chips = RenderChips([], null);

        Assert.Empty(chips.FindAll(".gwios-chip"));
        Assert.Contains("Noch keine Favoriten", chips.Find(".gwios-booking-template-chips-empty").TextContent);
    }

    private IRenderedComponent<GwiOSBookingTemplateChipsUnderTest> RenderChips(
        IReadOnlyList<BookingTemplate> templates,
        Guid? activeTemplateId)
        => Render<GwiOSBookingTemplateChipsUnderTest>(parameters => parameters
            .Add(component => component.Templates, templates)
            .Add(component => component.ActiveTemplateId, activeTemplateId)
            .Add(component => component.OnSelect, (BookingTemplate template) => _picked.Add(template)));
}
