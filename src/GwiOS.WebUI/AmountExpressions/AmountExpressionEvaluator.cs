using System.Globalization;
using System.Text.RegularExpressions;
using GwiOS.WebUI.AmountExpressions.Contracts;
using GwiOS.WebUI.AmountExpressions.Contracts.Models;

namespace GwiOS.WebUI.AmountExpressions;

internal sealed partial class AmountExpressionEvaluator : IAmountExpressionEvaluator
{
    private static readonly AmountExpressionResult InvalidResult = new();
    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

    public AmountExpressionResult Evaluate(string text)
    {
        string expression = Normalize(text);
        if (!ExpressionRegex().IsMatch(expression))
        {
            return InvalidResult;
        }

        MatchCollection terms = TermRegex().Matches(expression);
        decimal sum = 0;
        foreach (Match term in terms)
        {
            if (!TryParseNumber(term.Groups["number"].Value, out decimal number))
            {
                return InvalidResult;
            }

            sum += term.Groups["sign"].Value == "-" ? -number : number;
        }

        return new AmountExpressionResult
        {
            Value = Math.Round(sum, 2, MidpointRounding.AwayFromZero),
            IsCalculation = terms.Count > 1
        };
    }

    public string Format(decimal amount)
        => amount.ToString("0.00", GermanCulture);

    private static string Normalize(string text)
        => WhiteSpaceRegex().Replace(text, string.Empty).Replace('−', '-');

    // With a comma, dots group thousands (German); without one, a dot is taken as the decimal separator.
    private static bool TryParseNumber(string number, out decimal value)
    {
        value = 0;
        string invariantNumber = number.Contains(',') ? number.Replace(".", string.Empty).Replace(',', '.') : number;
        return InvariantNumberRegex().IsMatch(invariantNumber)
            && decimal.TryParse(invariantNumber, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
    }

    [GeneratedRegex(@"\s")]
    private static partial Regex WhiteSpaceRegex();

    // An optional leading sign, then numbers joined by single signs; a trailing or doubled sign ("5+", "5+-3") is
    // invalid.
    [GeneratedRegex(@"^[+\-]?[0-9.,]+([+\-][0-9.,]+)*$")]
    private static partial Regex ExpressionRegex();

    [GeneratedRegex(@"(?<sign>[+\-]?)(?<number>[0-9.,]+)")]
    private static partial Regex TermRegex();

    [GeneratedRegex(@"^(\d+(\.\d*)?|\.\d+)$")]
    private static partial Regex InvariantNumberRegex();
}
