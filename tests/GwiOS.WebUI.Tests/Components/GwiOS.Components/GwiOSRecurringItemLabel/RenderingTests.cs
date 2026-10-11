using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Models;
using GwiOSRecurringItemLabelUnderTest = GwiOS.WebUI.Components.GwiOS.Components.GwiOSRecurringItemLabel;

namespace GwiOS.WebUI.Tests.Components.GwiOS.Components.GwiOSRecurringItemLabel;

/// <summary>
/// Covers how <c>GwiOSRecurringItemLabel</c> shows the recurring item a booking was generated from.
/// </summary>
public sealed class RenderingTests : BunitContext
{
    private readonly RecurringItem _rent = new()
    {
        Name = "Miete",
        Type = CategoryType.Expense,
        CategoryId = Guid.NewGuid(),
        Amount = 1150m,
        Interval = RecurringInterval.Monthly
    };

    [Fact]
    public void ShowsTheNameOfTheRecurringItem()
    {
        IRenderedComponent<GwiOSRecurringItemLabelUnderTest> label = RenderLabel(_rent.Id);

        Assert.Equal("Miete", label.Find(".gwios-recurring-item-label-name").TextContent);
    }

    [Fact]
    public void ShowsAGenericName_WhenTheRecurringItemIsUnknown()
    {
        IRenderedComponent<GwiOSRecurringItemLabelUnderTest> label = RenderLabel(Guid.NewGuid());

        Assert.Equal("Fixposten", label.Find(".gwios-recurring-item-label-name").TextContent);
    }

    [Fact]
    public void RendersNothing_WhenThereIsNoRecurringItem()
    {
        IRenderedComponent<GwiOSRecurringItemLabelUnderTest> label = RenderLabel(null);

        Assert.Empty(label.FindAll(".gwios-recurring-item-label"));
    }

    private IRenderedComponent<GwiOSRecurringItemLabelUnderTest> RenderLabel(Guid? recurringItemId)
        => Render<GwiOSRecurringItemLabelUnderTest>(parameters => parameters
            .Add(component => component.RecurringItems, [_rent])
            .Add(component => component.RecurringItemId, recurringItemId));
}
