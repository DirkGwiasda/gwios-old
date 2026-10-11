using GwiOSAmountBoxUnderTest = GwiOS.WebUI.Components.GwiOS.Components.GwiOSAmountBox;

namespace GwiOS.WebUI.Tests.Components.GwiOS.Components.GwiOSAmountBox;

/// <summary>
/// Covers how <c>GwiOSAmountBox</c> reports what the user types.
/// </summary>
public sealed class InputTests : GwiOSAmountBoxProbe
{
    [Fact]
    public void ReportsTheTypedTextWithTheLastTwoDigitsAsCents()
    {
        IRenderedComponent<GwiOSAmountBoxUnderTest> amountBox = RenderAmountBox();

        amountBox.Find("input").Input("1250+3");

        Assert.Equal(["12,50+3"], ReportedValues);
    }

    [Fact]
    public void ShowsTheCentsWhileTyping()
    {
        IRenderedComponent<GwiOSAmountBoxUnderTest> amountBox = RenderAmountBox();

        amountBox.Find("input").Input("125");

        Assert.Equal("1,25", amountBox.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void DropsCharactersThatCannotBePartOfAnAmount()
    {
        IRenderedComponent<GwiOSAmountBoxUnderTest> amountBox = RenderAmountBox();

        amountBox.Find("input").Input("1a2 €,5x");

        Assert.Equal(["1,25"], ReportedValues);
    }

    [Fact]
    public void RaisesOnEnter_WhenEnterIsPressed()
    {
        IRenderedComponent<GwiOSAmountBoxUnderTest> amountBox = RenderAmountBox("5");

        amountBox.Find("input").KeyDown("Enter");

        Assert.Equal(1, EnterCount);
    }
}
