using GwiOS.Core.CrossCutting.Logging.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOS.WebUI.AmountExpressions;
using GwiOS.WebUI.AmountExpressions.Contracts;
using GwiOS.WebUI.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;
using GwiOSAmountBoxUnderTest = GwiOS.WebUI.Components.GwiOS.Components.GwiOSAmountBox;

namespace GwiOS.WebUI.Tests.Components.GwiOS.Components.GwiOSAmountBox;

/// <summary>
/// Renders a <c>GwiOSAmountBox</c> with the real amount evaluator and records every value it reports and every Enter.
/// </summary>
public abstract class GwiOSAmountBoxProbe : BunitContext
{
    protected GwiOSAmountBoxProbe()
    {
        Services.AddSingleton<ILogger<AmountExpressionEvaluator>, LoggerFake<AmountExpressionEvaluator>>();
        Services.AddSingleton<IAmountExpressionEvaluator, AmountExpressionEvaluator>();
    }

    /// <summary>
    /// The values reported through <c>ValueChanged</c>, in order.
    /// </summary>
    protected List<string> ReportedValues { get; } = [];

    /// <summary>
    /// How often <c>OnEnter</c> was raised.
    /// </summary>
    protected int EnterCount { get; private set; }

    protected IRenderedComponent<GwiOSAmountBoxUnderTest> RenderAmountBox(
        string value = "",
        CategoryType type = CategoryType.Expense,
        string? errorMessage = null,
        int focusRequest = 0)
        => Render<GwiOSAmountBoxUnderTest>(parameters => parameters
            .Add(component => component.InputId, "amount")
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, (string newValue) => ReportedValues.Add(newValue))
            .Add(component => component.Type, type)
            .Add(component => component.ErrorMessage, errorMessage)
            .Add(component => component.FocusRequest, focusRequest)
            .Add(component => component.OnEnter, () => EnterCount++));
}
