using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOSBookingTemplateListUnderTest = GwiOS.WebUI.Components.GwiOS.Components.GwiOSBookingTemplateList;

namespace GwiOS.WebUI.Tests.Components.GwiOS.Components.GwiOSBookingTemplateList;

/// <summary>
/// Renders a <c>GwiOSBookingTemplateList</c> with three favorites ("Edeka", "Bäcker Müller", "Aral") and records every
/// action it reports, e.g. "up Aral" or "rename Bäcker Müller (Ecke)".
/// </summary>
public abstract class GwiOSBookingTemplateListProbe : BunitContext
{
    protected GwiOSBookingTemplateListProbe()
    {
        Templates =
        [
            new() { Description = "Edeka", CategoryId = Groceries.Id, Position = 0 },
            new() { Description = "Bäcker Müller", CategoryId = Groceries.Id, Position = 1 },
            new() { Description = "Aral", CategoryId = Groceries.Id, Position = 2 }
        ];
    }

    protected Category Groceries { get; } =
        new() { Name = "Lebensmittel", Type = CategoryType.Expense, Color = "#ffd056" };

    protected List<string> Reports { get; } = [];

    /// <summary>
    /// The template copies reported through <c>OnRename</c>.
    /// </summary>
    protected List<BookingTemplate> RenamedTemplates { get; } = [];

    protected List<BookingTemplate> Templates { get; }

    protected IRenderedComponent<GwiOSBookingTemplateListUnderTest> RenderList(
        IReadOnlyList<BookingTemplate>? templates = null)
        => Render<GwiOSBookingTemplateListUnderTest>(parameters => parameters
            .Add(component => component.Templates, templates ?? Templates)
            .Add(component => component.Categories, [Groceries])
            .Add(component => component.OnMoveUp, (BookingTemplate template) => Reports.Add($"up {template.Description}"))
            .Add(
                component => component.OnMoveDown,
                (BookingTemplate template) => Reports.Add($"down {template.Description}"))
            .Add(component => component.OnRename, (BookingTemplate template) => RenamedTemplates.Add(template))
            .Add(
                component => component.OnDelete,
                (BookingTemplate template) => Reports.Add($"delete {template.Description}")));
}
