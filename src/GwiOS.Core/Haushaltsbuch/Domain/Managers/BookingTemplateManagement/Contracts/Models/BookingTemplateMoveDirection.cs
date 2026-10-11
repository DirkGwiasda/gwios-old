namespace GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Models;

/// <summary>
/// The direction in which a booking template moves within the ordered list of all booking templates.
/// </summary>
public enum BookingTemplateMoveDirection
{
    /// <summary>
    /// One place towards the start of the list, i.e. it swaps places with the template before it.
    /// </summary>
    Up,

    /// <summary>
    /// One place towards the end of the list, i.e. it swaps places with the template after it.
    /// </summary>
    Down
}
