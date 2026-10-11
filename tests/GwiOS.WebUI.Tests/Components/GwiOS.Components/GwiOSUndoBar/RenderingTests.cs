using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using GwiOSUndoBarUnderTest = GwiOS.WebUI.Components.GwiOS.Components.GwiOSUndoBar;

namespace GwiOS.WebUI.Tests.Components.GwiOS.Components.GwiOSUndoBar;

/// <summary>
/// Covers how <c>GwiOSUndoBar</c> shows its message and reports "Rückgängig".
/// </summary>
public sealed class RenderingTests : BunitContext
{
    private int _undoCount;

    public RenderingTests()
        => Services.AddSingleton<TimeProvider>(new FakeTimeProvider());

    [Fact]
    public void ShowsTheTextAndAnUndoButton()
    {
        IRenderedComponent<GwiOSUndoBarUnderTest> undoBar = RenderUndoBar(canUndo: true);

        Assert.Equal("Edeka · −47,83 € gebucht", undoBar.Find(".gwios-undo-bar-text").TextContent);
        Assert.Equal("Rückgängig", undoBar.Find("button").TextContent);
    }

    [Fact]
    public void ReportsTheUndo_WhenTheButtonIsClicked()
    {
        IRenderedComponent<GwiOSUndoBarUnderTest> undoBar = RenderUndoBar(canUndo: true);

        undoBar.Find("button").Click();

        Assert.Equal(1, _undoCount);
    }

    [Fact]
    public void ShowsNoUndoButton_WhenNothingCanBeUndone()
    {
        IRenderedComponent<GwiOSUndoBarUnderTest> undoBar = RenderUndoBar(canUndo: false);

        Assert.Empty(undoBar.FindAll("button"));
    }

    private IRenderedComponent<GwiOSUndoBarUnderTest> RenderUndoBar(bool canUndo)
        => Render<GwiOSUndoBarUnderTest>(parameters => parameters
            .Add(component => component.Text, "Edeka · −47,83 € gebucht")
            .Add(component => component.CanUndo, canUndo)
            .Add(component => component.OnUndo, () => _undoCount++));
}
