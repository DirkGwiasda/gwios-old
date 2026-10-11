using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Models;
using Microsoft.AspNetCore.Components;

namespace GwiOS.WebUI.Components.GwiOS.Components;

/// <summary>
/// Shows the name of the recurring item a booking was generated from, e.g. "↻ Miete"; renders nothing if the booking
/// was not generated from a recurring item.
/// </summary>
public partial class GwiOSRecurringItemLabel
{
    // Shown if the referenced recurring item is not among the given ones, so the origin is not lost.
    private const string UnknownName = "Fixposten";

    private string? _name;

    /// <summary>
    /// All recurring items; the name is looked up among them.
    /// </summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<RecurringItem> RecurringItems { get; set; } = [];

    /// <summary>
    /// The ID of the recurring item to show, or <c>null</c> to show nothing.
    /// </summary>
    [Parameter, EditorRequired]
    public Guid? RecurringItemId { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
        => _name = RecurringItemId is Guid id
            ? RecurringItems.FirstOrDefault(item => item.Id == id)?.Name ?? UnknownName
            : null;
}
