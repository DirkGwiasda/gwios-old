using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOS.WebUI.AmountExpressions.Contracts;
using GwiOS.WebUI.AmountExpressions.Contracts.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace GwiOS.WebUI.Components.GwiOS.Components;

/// <summary>
/// Large amount field of the booking entry: shows the sign of the booking type, opens the numeric keyboard on phones,
/// and accepts short calculations such as <c>12,50+3,20</c>, whose result it shows below the field. While typing,
/// the text is formatted with <see cref="IAmountExpressionEvaluator.FormatInput"/>: only digits and signs are kept,
/// and from three digits on the last two are the cents (<c>1250</c> shows as <c>12,50</c>). The text itself is the
/// value; the caller evaluates it with <see cref="IAmountExpressionEvaluator"/> when saving.
/// </summary>
public partial class GwiOSAmountBox
{
    private ElementReference _inputElement;
    private AmountExpressionResult _result = new();
    private int _handledFocusRequest;

    [Inject]
    private IAmountExpressionEvaluator AmountExpressionEvaluator { get; set; } = default!;

    /// <summary>
    /// The text in the field, e.g. <c>47,83</c> or <c>12,50+3,20</c>. Never <c>null</c>; empty if nothing is entered.
    /// </summary>
    [Parameter]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Raised with the new text on every change, so the value can be bound with <c>@bind-Value</c>.
    /// </summary>
    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    /// <summary>
    /// The type of the booking; decides the sign in front of the amount (− for an expense, + for income).
    /// </summary>
    [Parameter]
    public CategoryType Type { get; set; } = CategoryType.Expense;

    /// <summary>
    /// The <c>id</c> of the input element, so that a label can refer to it.
    /// </summary>
    [Parameter, EditorRequired]
    public string InputId { get; set; } = string.Empty;

    /// <summary>
    /// Grey text shown while the field is empty.
    /// </summary>
    [Parameter]
    public string Placeholder { get; set; } = "0,00";

    /// <summary>
    /// Explanation below the field, shown while there is neither an error nor a calculation result.
    /// </summary>
    [Parameter]
    public string Hint { get; set; } = "Ab drei Ziffern sind die letzten beiden Cent: 1250 → 12,50. Rechnen mit + und −.";

    /// <summary>
    /// Error shown below the field and marking it as invalid, or <c>null</c> while the value is valid.
    /// </summary>
    [Parameter]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Raised when the user presses Enter in the field, typically to save.
    /// </summary>
    [Parameter]
    public EventCallback OnEnter { get; set; }

    /// <summary>
    /// A counter the caller increases whenever the field should take the keyboard focus, e.g. when the form opens or
    /// a favorite was picked. The field focuses itself after rendering with a value it has not handled yet, so a
    /// value above 0 on the first render focuses it right away.
    /// </summary>
    [Parameter]
    public int FocusRequest { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
        => _result = AmountExpressionEvaluator.Evaluate(Value);

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (FocusRequest == _handledFocusRequest)
        {
            return;
        }

        _handledFocusRequest = FocusRequest;
        await _inputElement.FocusAsync();
    }

    private async Task OnInputAsync(ChangeEventArgs eventArgs)
    {
        string typedText = eventArgs.Value?.ToString() ?? string.Empty;
        string formattedText = AmountExpressionEvaluator.FormatInput(typedText);
        if ((formattedText == Value) && (formattedText != typedText))
        {
            // A dropped character (e.g. a comma) leaves the formatted text unchanged, so a render would not write it
            // back and the field would keep showing the character. Rendering the typed text first makes the second
            // render replace it.
            Value = typedText;
            await Task.Yield();
        }

        await UpdateValueAsync(formattedText);
    }

    private async Task OnKeyDownAsync(KeyboardEventArgs eventArgs)
    {
        if (eventArgs.Key == "Enter")
        {
            await OnEnter.InvokeAsync();
        }
    }

    private async Task UpdateValueAsync(string value)
    {
        Value = value;
        _result = AmountExpressionEvaluator.Evaluate(value);
        await ValueChanged.InvokeAsync(value);
    }
}
