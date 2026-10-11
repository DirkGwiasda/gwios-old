using GwiOS.WebUI.AmountExpressions.Contracts.Models;

namespace GwiOS.WebUI.AmountExpressions.Contracts;

/// <summary>
/// Evaluates an amount the user typed into an amount field: a single number or a short sum such as
/// <c>12,50+3,20-1</c>, as written in German.
/// </summary>
public interface IAmountExpressionEvaluator
{
    /// <summary>
    /// Evaluates the given text. Numbers may use a comma as decimal separator (then dots are thousands separators,
    /// e.g. <c>1.234,50</c>) or, without a comma, a dot (<c>12.5</c>). Numbers are joined by <c>+</c>, <c>-</c> or the
    /// minus sign <c>−</c>; white space is ignored. Any other character makes the text invalid.
    /// </summary>
    AmountExpressionResult Evaluate(string text);

    /// <summary>
    /// Formats an amount the way the user would type it, e.g. <c>47,83</c> for 47.83 — two decimals, comma as decimal
    /// separator, no thousands separator and no currency.
    /// </summary>
    string Format(decimal amount);
}
