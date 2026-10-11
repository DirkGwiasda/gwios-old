namespace GwiOS.WebUI.AmountExpressions.Contracts.Models;

/// <summary>
/// The outcome of evaluating an amount the user typed, e.g. <c>47,83</c> or <c>12,50+3,20</c>.
/// </summary>
public sealed class AmountExpressionResult
{
    /// <summary>
    /// The result of the expression in euros, rounded to cents; <c>null</c> if the text is empty or not a valid
    /// expression. May be zero or negative (e.g. <c>5-8</c>); whether that is acceptable is up to the caller.
    /// </summary>
    public decimal? Value { get; init; }

    /// <summary>
    /// Whether the text is a calculation, i.e. contains a <c>+</c> or <c>-</c> between two numbers, rather than a
    /// single number. Callers show the result only for a calculation, since a single number needs no result.
    /// </summary>
    public bool IsCalculation { get; init; }
}
