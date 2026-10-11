using GwiOS.WebUI.Tests.TestInfrastructure;
using AmountExpressionEvaluatorUnderTest = GwiOS.WebUI.AmountExpressions.AmountExpressionEvaluator;

namespace GwiOS.WebUI.Tests.AmountExpressions.AmountExpressionEvaluator;

/// <summary>
/// Covers <c>AmountExpressionEvaluator.FormatInput</c>.
/// </summary>
public sealed class FormatInputTests
{
    private readonly AmountExpressionEvaluatorUnderTest _evaluator = new(new LoggerFake<AmountExpressionEvaluatorUnderTest>());

    [Theory]
    [InlineData("1", "1")]
    [InlineData("12", "12")]
    [InlineData("125", "1,25")]
    [InlineData("1250", "12,50")]
    [InlineData("123456", "1234,56")]
    public void MakesTheLastTwoDigitsTheCents_FromThreeDigitsOn(string text, string expected)
    {
        Assert.Equal(expected, _evaluator.FormatInput(text));
    }

    [Theory]
    [InlineData("12,50", "12,50")]
    [InlineData("12,500", "125,00")]
    [InlineData("12,5", "1,25")]
    [InlineData("1,2", "12")]
    [InlineData("1.234,56", "1234,56")]
    public void IgnoresTypedSeparatorsAndPlacesTheCommaItself(string text, string expected)
    {
        Assert.Equal(expected, _evaluator.FormatInput(text));
    }

    [Theory]
    [InlineData("005", "0,05")]
    [InlineData("0050", "0,50")]
    [InlineData("01250", "12,50")]
    public void DropsLeadingZerosOfTheEurosButKeepsOne(string text, string expected)
    {
        Assert.Equal(expected, _evaluator.FormatInput(text));
    }

    [Theory]
    [InlineData("1250+320", "12,50+3,20")]
    [InlineData("1250-5", "12,50-5")]
    [InlineData("1250−320", "12,50−3,20")]
    [InlineData("-1250", "-12,50")]
    [InlineData("1250+", "12,50+")]
    public void FormatsEveryNumberOfACalculationOnItsOwn(string text, string expected)
    {
        Assert.Equal(expected, _evaluator.FormatInput(text));
    }

    [Fact]
    public void DropsAllOtherCharacters()
    {
        Assert.Equal("12,50", _evaluator.FormatInput("1a2 €5x0"));
    }

    [Fact]
    public void LeavesFormattedAmountsUnchanged()
    {
        Assert.Equal("47,83", _evaluator.FormatInput(_evaluator.Format(47.83m)));
        Assert.Equal("5,00", _evaluator.FormatInput(_evaluator.Format(5m)));
    }
}
