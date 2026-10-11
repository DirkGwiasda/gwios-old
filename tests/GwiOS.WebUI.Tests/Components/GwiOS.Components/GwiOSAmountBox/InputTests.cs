using GwiOSAmountBoxUnderTest = GwiOS.WebUI.Components.GwiOS.Components.GwiOSAmountBox;

namespace GwiOS.WebUI.Tests.Components.GwiOS.Components.GwiOSAmountBox;

/// <summary>
/// Covers how <c>GwiOSAmountBox</c> reports what the user types and clicks.
/// </summary>
public sealed class InputTests : GwiOSAmountBoxProbe
{
    [Fact]
    public void ReportsTheTypedText()
    {
        IRenderedComponent<GwiOSAmountBoxUnderTest> amountBox = RenderAmountBox();

        amountBox.Find("input").Input("12,50+3");

        Assert.Equal(["12,50+3"], ReportedValues);
    }

    [Fact]
    public void DropsCharactersThatCannotBePartOfAnAmount()
    {
        IRenderedComponent<GwiOSAmountBoxUnderTest> amountBox = RenderAmountBox();

        amountBox.Find("input").Input("1a2 €,5x");

        Assert.Equal(["12,5"], ReportedValues);
    }

    [Fact]
    public void AppendsAPlus_WhenThePlusButtonIsClicked()
    {
        IRenderedComponent<GwiOSAmountBoxUnderTest> amountBox = RenderAmountBox("12,50");

        amountBox.Find("button[aria-label='Plus rechnen']").Click();

        Assert.Equal(["12,50+"], ReportedValues);
    }

    [Fact]
    public void ReplacesATrailingSign_WhenAnotherSignIsClicked()
    {
        IRenderedComponent<GwiOSAmountBoxUnderTest> amountBox = RenderAmountBox("12,50+");

        amountBox.Find("button[aria-label='Minus rechnen']").Click();

        Assert.Equal(["12,50-"], ReportedValues);
    }

    [Fact]
    public void RaisesOnEnter_WhenEnterIsPressed()
    {
        IRenderedComponent<GwiOSAmountBoxUnderTest> amountBox = RenderAmountBox("5");

        amountBox.Find("input").KeyDown("Enter");

        Assert.Equal(1, EnterCount);
    }
}
