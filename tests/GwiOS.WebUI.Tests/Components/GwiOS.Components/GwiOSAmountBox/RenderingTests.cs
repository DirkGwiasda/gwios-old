using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOSAmountBoxUnderTest = GwiOS.WebUI.Components.GwiOS.Components.GwiOSAmountBox;

namespace GwiOS.WebUI.Tests.Components.GwiOS.Components.GwiOSAmountBox;

/// <summary>
/// Covers how <c>GwiOSAmountBox</c> renders the amount, its sign, the result of a calculation, hints and errors.
/// </summary>
public sealed class RenderingTests : GwiOSAmountBoxProbe
{
    [Fact]
    public void OpensTheNumericKeyboardOnPhones()
    {
        IRenderedComponent<GwiOSAmountBoxUnderTest> amountBox = RenderAmountBox();

        Assert.Equal("decimal", amountBox.Find("input").GetAttribute("inputmode"));
        Assert.Equal("amount", amountBox.Find("input").Id);
    }

    [Theory]
    [InlineData(CategoryType.Expense, "−")]
    [InlineData(CategoryType.Income, "+")]
    public void ShowsTheSignOfTheBookingType(CategoryType type, string expectedSign)
    {
        IRenderedComponent<GwiOSAmountBoxUnderTest> amountBox = RenderAmountBox(type: type);

        Assert.Equal(expectedSign, amountBox.Find(".gwios-amount-box-sign").TextContent.Trim());
    }

    [Fact]
    public void ShowsTheResultOfACalculation()
    {
        IRenderedComponent<GwiOSAmountBoxUnderTest> amountBox = RenderAmountBox("12,50+3,20");

        Assert.Equal("= 15,70 €", amountBox.Find(".gwios-amount-box-result").TextContent);
    }

    [Fact]
    public void ShowsTheHintInsteadOfAResult_WhenTheValueIsASingleNumber()
    {
        IRenderedComponent<GwiOSAmountBoxUnderTest> amountBox = RenderAmountBox("47,83");

        Assert.Empty(amountBox.FindAll(".gwios-amount-box-result"));
        Assert.Equal("Rechnen geht auch, z. B. 12,50+3,20", amountBox.Find(".gwios-amount-box-hint").TextContent);
    }

    [Fact]
    public void ShowsTheErrorAndMarksTheFieldInvalid_WhenAnErrorIsGiven()
    {
        IRenderedComponent<GwiOSAmountBoxUnderTest> amountBox =
            RenderAmountBox("0", errorMessage: "Der Betrag muss größer als 0 sein.");

        Assert.Equal("Der Betrag muss größer als 0 sein.", amountBox.Find(".gwios-amount-box-error").TextContent);
        Assert.Equal("true", amountBox.Find("input").GetAttribute("aria-invalid"));
        Assert.Empty(amountBox.FindAll(".gwios-amount-box-hint"));
    }

    [Fact]
    public void TakesTheFocus_WhenAFocusRequestIsPending()
    {
        RenderAmountBox(focusRequest: 1);

        JSInterop.VerifyFocusAsyncInvoke();
    }

    [Fact]
    public void DoesNotTakeTheFocus_WithoutAFocusRequest()
    {
        RenderAmountBox();

        JSInterop.VerifyNotInvoke("Blazor._internal.domWrapper.focus");
    }
}
