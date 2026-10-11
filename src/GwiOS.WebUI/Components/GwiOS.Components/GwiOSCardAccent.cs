namespace GwiOS.WebUI.Components.GwiOS.Components;

/// <summary>
/// Color of the title of a <see cref="GwiOSCard"/>.
/// </summary>
public enum GwiOSCardAccent
{
    /// <summary>
    /// The primary blue, for regular content.
    /// </summary>
    Primary = 0,

    /// <summary>
    /// Green, for positive content such as income.
    /// </summary>
    Success = 1,

    /// <summary>
    /// Red, for negative content such as expenses.
    /// </summary>
    Danger = 2,

    /// <summary>
    /// Yellow, for favorites, matching their star.
    /// </summary>
    Warning = 3
}
