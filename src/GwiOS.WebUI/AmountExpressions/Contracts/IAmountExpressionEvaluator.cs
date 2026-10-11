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

    /// <summary>
    /// Brings what the user typed into the form the field shows, keystroke by keystroke: only digits and the signs
    /// <c>+</c>, <c>-</c> and <c>−</c> are kept, and in every number of at least three digits the last two digits
    /// are the cents, so typing <c>1</c>, <c>2</c>, <c>5</c>, <c>0</c> shows <c>1</c>, <c>12</c>, <c>1,25</c>,
    /// <c>12,50</c>. Numbers of one or two digits stay whole euros. A typed comma or dot is dropped, because the
    /// decimal comma is always placed this way; an amount already in this form (e.g. from <see cref="Format"/>)
    /// stays unchanged.
    /// </summary>
    string FormatInput(string text);
}
