using GwiOS.WebUI.AmountExpressions.Contracts.Models;
using AmountExpressionEvaluatorUnderTest = GwiOS.WebUI.AmountExpressions.AmountExpressionEvaluator;

namespace GwiOS.WebUI.Tests.AmountExpressions.AmountExpressionEvaluator;

/// <summary>
/// Covers <c>AmountExpressionEvaluator.Evaluate</c>.
/// </summary>
public sealed class EvaluateTests
{
    private readonly AmountExpressionEvaluatorUnderTest _evaluator = new();

    [Theory]
    [InlineData("47,83", 47.83)]
    [InlineData("47", 47)]
    [InlineData("12.5", 12.5)]
    [InlineData("1.234,50", 1234.50)]
    [InlineData(" 6,40 ", 6.40)]
    [InlineData(",5", 0.5)]
    public void ReturnsTheValueOfASingleNumberWithoutMarkingItAsCalculation(string text, double expected)
    {
        AmountExpressionResult result = _evaluator.Evaluate(text);

        Assert.Equal((decimal)expected, result.Value);
        Assert.False(result.IsCalculation);
    }

    [Theory]
    [InlineData("12,50+3,20", 15.70)]
    [InlineData("20-4,5", 15.5)]
    [InlineData("10 − 2,5 + 1", 8.5)]
    [InlineData("1+1+1", 3)]
    public void ReturnsTheSumOfACalculationAndMarksItAsCalculation(string text, double expected)
    {
        AmountExpressionResult result = _evaluator.Evaluate(text);

        Assert.Equal((decimal)expected, result.Value);
        Assert.True(result.IsCalculation);
    }

    [Fact]
    public void RoundsTheResultToCents()
    {
        AmountExpressionResult result = _evaluator.Evaluate("0,005+0,001");

        Assert.Equal(0.01m, result.Value);
    }

    [Fact]
    public void ReturnsAZeroOrNegativeResultAsItIs()
    {
        Assert.Equal(-3m, _evaluator.Evaluate("5-8").Value);
        Assert.Equal(0m, _evaluator.Evaluate("0").Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12,50+")]
    [InlineData("5+-3")]
    [InlineData("abc")]
    [InlineData("12€")]
    [InlineData("1,2,3")]
    [InlineData("1..2")]
    [InlineData("+")]
    public void ReturnsNoValue_WhenTheTextIsNotAValidAmount(string text)
    {
        AmountExpressionResult result = _evaluator.Evaluate(text);

        Assert.Null(result.Value);
        Assert.False(result.IsCalculation);
    }
}
