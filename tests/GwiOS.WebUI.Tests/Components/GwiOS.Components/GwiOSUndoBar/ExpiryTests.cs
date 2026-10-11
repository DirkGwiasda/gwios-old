using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using GwiOSUndoBarUnderTest = GwiOS.WebUI.Components.GwiOS.Components.GwiOSUndoBar;

namespace GwiOS.WebUI.Tests.Components.GwiOS.Components.GwiOSUndoBar;

/// <summary>
/// Covers how <c>GwiOSUndoBar</c> asks to be hidden once its duration has passed.
/// </summary>
public sealed class ExpiryTests : BunitContext
{
    private readonly FakeTimeProvider _timeProvider = new();
    private int _expiredCount;

    public ExpiryTests()
        => Services.AddSingleton<TimeProvider>(_timeProvider);

    [Fact]
    public void DoesNotExpireBeforeTheDurationHasPassed()
    {
        RenderUndoBar();

        _timeProvider.Advance(TimeSpan.FromSeconds(5.9));

        Assert.Equal(0, _expiredCount);
    }

    [Fact]
    public void ReportsTheExpiry_OnceTheDurationHasPassed()
    {
        IRenderedComponent<GwiOSUndoBarUnderTest> undoBar = RenderUndoBar();

        _timeProvider.Advance(TimeSpan.FromSeconds(6));

        undoBar.WaitForAssertion(() => Assert.Equal(1, _expiredCount));
    }

    [Fact]
    public void NeverReportsTheExpiry_WhenDisposedBefore()
    {
        IRenderedComponent<GwiOSUndoBarUnderTest> undoBar = RenderUndoBar();

        undoBar.Instance.Dispose();
        _timeProvider.Advance(TimeSpan.FromSeconds(10));

        Assert.Equal(0, _expiredCount);
    }

    private IRenderedComponent<GwiOSUndoBarUnderTest> RenderUndoBar()
        => Render<GwiOSUndoBarUnderTest>(parameters => parameters
            .Add(component => component.Text, "Edeka · −47,83 € gebucht")
            .Add(component => component.Duration, TimeSpan.FromSeconds(6))
            .Add(component => component.OnExpired, () => _expiredCount++));
}
