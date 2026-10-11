using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using GwiOS.Core.CrossCutting.Logging.Contracts;
using GwiOS.WebUI.AmountExpressions.Contracts;
using GwiOS.WebUI.AmountExpressions.Contracts.Models;

namespace GwiOS.WebUI.AmountExpressions;

internal sealed partial class AmountExpressionEvaluator(ILogger<AmountExpressionEvaluator> logger)
    : IAmountExpressionEvaluator
{
    private static readonly AmountExpressionResult InvalidResult = new();
    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

    private readonly ILogger<AmountExpressionEvaluator> _logger = logger;

    public AmountExpressionResult Evaluate(string text)
    {
        string expression = Normalize(text);
        if (!ExpressionRegex().IsMatch(expression))
        {
            return LogInvalid();
        }

        MatchCollection terms = TermRegex().Matches(expression);
        decimal sum = 0;
        foreach (Match term in terms)
        {
            if (!TryParseNumber(term.Groups["number"].Value, out decimal number))
            {
                return LogInvalid();
            }

            sum += term.Groups["sign"].Value == "-" ? -number : number;
        }

        return new AmountExpressionResult
        {
            Value = Math.Round(sum, 2, MidpointRounding.AwayFromZero),
            IsCalculation = terms.Count > 1
        };
    }

    // The amount itself is not logged; it is private data of the family.
    private AmountExpressionResult LogInvalid()
    {
        _logger.LogDebug("Amount expression is invalid.");
        return InvalidResult;
    }

    public string Format(decimal amount)
        => amount.ToString("0.00", GermanCulture);

    public string FormatInput(string text)
    {
        StringBuilder result = new();
        StringBuilder digits = new();
        foreach (char character in text)
        {
            if (char.IsAsciiDigit(character))
            {
                digits.Append(character);
            }
            else if (character is '+' or '-' or '−')
            {
                AppendNumber(result, digits);
                result.Append(character);
            }
        }

        AppendNumber(result, digits);
        return result.ToString();
    }

    // From three digits on, the last two are the cents; leading zeros of the euros go, except a single one ("0,05").
    private static void AppendNumber(StringBuilder result, StringBuilder digits)
    {
        string number = digits.ToString();
        digits.Clear();
        if (number.Length < 3)
        {
            result.Append(number);
            return;
        }

        string euros = number[..^2].TrimStart('0');
        result.Append(euros.Length == 0 ? "0" : euros).Append(',').Append(number[^2..]);
    }

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
