using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace GwiOS.WebUI.Components.GwiOS.Components;

/// <summary>
/// The list of booking templates (favorites) with their categories, where they are moved up and down, renamed in
/// place and deleted after a confirming second click. Arrows instead of drag and drop, so it works the same on a
/// phone. The component only reports the actions; storing them is up to the caller.
/// </summary>
public partial class GwiOSBookingTemplateList
{
    private Guid? _renamingTemplateId;
    private string _renameText = string.Empty;
    private ElementReference _renameInput;
    private bool _shouldFocusRenameInput;

    /// <summary>
    /// The templates in the order they are offered in.
    /// </summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<BookingTemplate> Templates { get; set; } = [];

    /// <summary>
    /// All categories, flat; each template shows the path of its category.
    /// </summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<Category> Categories { get; set; } = [];

    /// <summary>
    /// Raised with the template to move one place up. Never raised for the first template.
    /// </summary>
    [Parameter]
    public EventCallback<BookingTemplate> OnMoveUp { get; set; }

    /// <summary>
    /// Raised with the template to move one place down. Never raised for the last template.
    /// </summary>
    [Parameter]
    public EventCallback<BookingTemplate> OnMoveDown { get; set; }

    /// <summary>
    /// Raised with a copy of the renamed template: same ID, category and position, the new, trimmed description.
    /// Not raised if the new description is empty or unchanged.
    /// </summary>
    [Parameter]
    public EventCallback<BookingTemplate> OnRename { get; set; }

    /// <summary>
    /// Raised with the template to delete, after the user confirmed.
    /// </summary>
    [Parameter]
    public EventCallback<BookingTemplate> OnDelete { get; set; }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_shouldFocusRenameInput)
        {
            _shouldFocusRenameInput = false;
            await _renameInput.FocusAsync();
        }
    }

    private void StartRename(BookingTemplate template)
    {
        _renamingTemplateId = template.Id;
        _renameText = template.Description;
        _shouldFocusRenameInput = true;
    }

    private void CancelRename()
        => _renamingTemplateId = null;

    private async Task OnRenameKeyDownAsync(KeyboardEventArgs eventArgs)
    {
        if (eventArgs.Key == "Enter")
        {
            await SaveRenameAsync();
        }
        else if (eventArgs.Key == "Escape")
        {
            CancelRename();
        }
    }

    private async Task SaveRenameAsync()
    {
        BookingTemplate? template = Templates.FirstOrDefault(candidate => candidate.Id == _renamingTemplateId);
        string description = _renameText.Trim();
        if ((template is null) || (description.Length == 0))
        {
            return;
        }

        _renamingTemplateId = null;
        if (description != template.Description)
        {
            await OnRename.InvokeAsync(new BookingTemplate
            {
                Id = template.Id, Description = description, CategoryId = template.CategoryId,
                Position = template.Position
            });
        }
    }
}
