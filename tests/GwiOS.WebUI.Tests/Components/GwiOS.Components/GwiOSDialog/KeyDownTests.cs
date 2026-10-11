using GwiOSDialogUnderTest = GwiOS.WebUI.Components.GwiOS.Components.GwiOSDialog;

namespace GwiOS.WebUI.Tests.Components.GwiOS.Components.GwiOSDialog;

/// <summary>
/// Covers how <c>GwiOSDialog</c> reacts to keys pressed inside it.
/// </summary>
public sealed class KeyDownTests : BunitContext
{
    private int _closeCount;

    [Fact]
    public void RaisesOnClose_WhenEscapeIsPressed()
    {
        IRenderedComponent<GwiOSDialogUnderTest> dialog = RenderDialog(canClose: true);

        dialog.Find("[role='dialog']").KeyDown("Escape");

        Assert.Equal(1, _closeCount);
    }

    [Fact]
    public void RaisesNoOnClose_WhenAnotherKeyIsPressed()
    {
        IRenderedComponent<GwiOSDialogUnderTest> dialog = RenderDialog(canClose: true);

        dialog.Find("[role='dialog']").KeyDown("Enter");

        Assert.Equal(0, _closeCount);
    }

    [Fact]
    public void RaisesNoOnClose_WhenEscapeIsPressedWhileTheDialogCannotBeClosed()
    {
        IRenderedComponent<GwiOSDialogUnderTest> dialog = RenderDialog(canClose: false);

        dialog.Find("[role='dialog']").KeyDown("Escape");

        Assert.Equal(0, _closeCount);
    }

    [Fact]
    public void ClosesOnlyTheInnerDialog_WhenEscapeIsPressedInADialogInsideAnother()
    {
        int innerCloseCount = 0;
        IRenderedComponent<GwiOSDialogUnderTest> dialog = Render<GwiOSDialogUnderTest>(parameters => parameters
            .Add(component => component.Title, "Buchung")
            .Add(component => component.OnClose, () => _closeCount++)
            .AddChildContent<GwiOSDialogUnderTest>(inner => inner
                .Add(component => component.Title, "Kategorie wählen")
                .Add(component => component.OnClose, () => innerCloseCount++)));

        dialog.FindAll("[role='dialog']")[1].KeyDown("Escape");

        Assert.Equal(1, innerCloseCount);
        Assert.Equal(0, _closeCount);
    }

    private IRenderedComponent<GwiOSDialogUnderTest> RenderDialog(bool canClose)
        => Render<GwiOSDialogUnderTest>(parameters => parameters
            .Add(component => component.Title, "Person bearbeiten")
            .Add(component => component.CanClose, canClose)
            .Add(component => component.OnClose, () => _closeCount++));
}
