using Microsoft.AspNetCore.Components;

namespace GwiOS.WebUI.Components.GwiOS.Components;

/// <summary>
/// Short-lived bar above the status bar that confirms an action and offers to take it back, e.g.
/// "Edeka · −47,83 € gebucht · Rückgängig". It asks to be hidden once <see cref="Duration"/> has passed since it was
/// shown; to show a new message with a fresh duration, the caller renders a new instance (e.g. with a new
/// <c>@key</c>).
/// </summary>
public sealed partial class GwiOSUndoBar : IDisposable
{
    private readonly CancellationTokenSource _disposal = new();
    private bool _isDisposed;

    [Inject]
    private TimeProvider TimeProvider { get; set; } = default!;

    /// <summary>
    /// The confirmation shown in the bar.
    /// </summary>
    [Parameter, EditorRequired]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Whether the bar offers "Rückgängig". <c>false</c> once the action was taken back, to confirm that.
    /// </summary>
    [Parameter]
    public bool CanUndo { get; set; } = true;

    /// <summary>
    /// How long the bar stays before it asks to be hidden.
    /// </summary>
    [Parameter]
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(6);

    /// <summary>
    /// Raised when the user clicks "Rückgängig".
    /// </summary>
    [Parameter]
    public EventCallback OnUndo { get; set; }

    /// <summary>
    /// Raised once <see cref="Duration"/> has passed; the bar stays until the caller stops rendering it.
    /// </summary>
    [Parameter]
    public EventCallback OnExpired { get; set; }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _disposal.Cancel();
        _disposal.Dispose();
    }

    /// <inheritdoc />
    protected override void OnInitialized()
        => _ = ExpireAfterDurationAsync(_disposal.Token);

    private async Task ExpireAfterDurationAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(Duration, TimeProvider, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await InvokeAsync(() => OnExpired.InvokeAsync());
    }
}
