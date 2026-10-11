using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace GwiOS.WebUI.Components.GwiOS.Components;

/// <summary>
/// Picks a date with a single tap: "Heute", "Gestern" and "Vorgestern" as chips, any other date through a calendar
/// chip that then shows the chosen date. Meant for entering bookings, which mostly happen today or were just
/// forgotten for a day or two.
/// </summary>
public partial class GwiOSDateChips
{
    private static readonly (string Label, int DaysAgo)[] RelativeDays = [("Heute", 0), ("Gestern", 1), ("Vorgestern", 2)];

    /// <summary>
    /// The selected date.
    /// </summary>
    [Parameter]
    public DateOnly Value { get; set; }

    /// <summary>
    /// Raised with the date the user selects, so the value can be bound with <c>@bind-Value</c>.
    /// </summary>
    [Parameter]
    public EventCallback<DateOnly> ValueChanged { get; set; }

    /// <summary>
    /// Today's date in the user's time zone; "Heute", "Gestern" and "Vorgestern" are counted from it.
    /// </summary>
    [Parameter, EditorRequired]
    public DateOnly Today { get; set; }

    /// <summary>
    /// Accessible name of the group.
    /// </summary>
    [Parameter]
    public string AriaLabel { get; set; } = "Datum";

    private bool IsCustomDate
        => (Value > Today) || (Value < Today.AddDays(-(RelativeDays.Length - 1)));

    private async Task SelectAsync(DateOnly date)
    {
        Value = date;
        await ValueChanged.InvokeAsync(date);
    }

    // Clearing the calendar field reports an empty value; the last date then simply stays selected.
    private async Task OnPickerChangeAsync(ChangeEventArgs eventArgs)
    {
        if (DateOnly.TryParseExact(
                eventArgs.Value?.ToString(),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateOnly date))
        {
            await SelectAsync(date);
        }
    }
}
