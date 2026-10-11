using AmountExpressionEvaluatorUnderTest = GwiOS.WebUI.AmountExpressions.AmountExpressionEvaluator;

namespace GwiOS.WebUI.Tests.AmountExpressions.AmountExpressionEvaluator;

/// <summary>
/// Covers <c>AmountExpressionEvaluator.Format</c>.
/// </summary>
public sealed class FormatTests
{
    private readonly AmountExpressionEvaluatorUnderTest _evaluator = new();

    [Theory]
    [InlineData(47.83, "47,83")]
    [InlineData(6.4, "6,40")]
    [InlineData(1234.5, "1234,50")]
    [InlineData(0, "0,00")]
    public void FormatsWithTwoDecimalsAndACommaButWithoutThousandsSeparator(double amount, string expected)
    {
        Assert.Equal(expected, _evaluator.Format((decimal)amount));
    }

    [Fact]
    public void ProducesTextThatEvaluatesToTheSameAmount()
    {
        Assert.Equal(1234.56m, _evaluator.Evaluate(_evaluator.Format(1234.56m)).Value);
    }
}
