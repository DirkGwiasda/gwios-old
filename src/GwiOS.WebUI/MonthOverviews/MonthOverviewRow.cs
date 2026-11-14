namespace GwiOS.WebUI.MonthOverviews;

/// <summary>
/// One line of a month overview as dialog and PDF show it: a category, or the share booked directly in a category
/// that also has sub-categories. Public, because the dialog reports a clicked line to the page.
/// </summary>
/// <param name="CategoryId">The ID of the category of the line.</param>
/// <param name="Name">The text of the line, e.g. <c>Lebensmittel</c> or <c>Haushalt (direkt)</c>.</param>
/// <param name="Depth">0 for a root category, 1 for its sub-categories, and so on; decides the amount column.</param>
/// <param name="Amount">The amount of the line: a category including its sub-categories, or the direct share.</param>
/// <param name="Color">The color of the root category, as CSS color.</param>
/// <param name="IsDirectShare">Whether the line is the direct share of the category above it.</param>
public sealed record MonthOverviewRow(
    Guid CategoryId, string Name, int Depth, decimal Amount, string Color, bool IsDirectShare);
