using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Models;

namespace GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts;

/// <summary>
/// Checks the values of a single booking template, and that no other booking template already stands for the same
/// description and category.
/// </summary>
public interface IBookingTemplateValidator
{
    /// <summary>
    /// Returns whether the given booking template is valid with respect to the already stored
    /// <paramref name="existingBookingTemplates"/>, which may include the template itself (when it is changed).
    /// <paramref name="validationErrors"/> receives one message per violated rule; it is empty if the template is
    /// valid.
    /// </summary>
    bool IsValid(
        BookingTemplate bookingTemplate,
        IReadOnlyList<BookingTemplate> existingBookingTemplates,
        out List<string> validationErrors);
}
