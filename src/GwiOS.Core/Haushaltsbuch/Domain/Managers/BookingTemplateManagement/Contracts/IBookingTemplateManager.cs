using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Exceptions;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Models;

namespace GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts;

/// <summary>
/// Manages the booking templates (favorites) of the Haushaltsbuch: creates, renames, reorders, reads and deletes
/// them, and rejects templates that are invalid.
/// </summary>
public interface IBookingTemplateManager
{
    /// <summary>
    /// Validates the given new booking template, places it behind all stored templates (overwriting its
    /// <see cref="BookingTemplate.Position"/>), stores it and returns it. Throws a
    /// <see cref="BookingTemplateValidationException"/> if the template is invalid, e.g. if another template already
    /// stands for the same description and category.
    /// </summary>
    Task<BookingTemplate> CreateBookingTemplateAsync(
        BookingTemplate bookingTemplate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates and stores the description and category of the given changed booking template; its place in the
    /// list stays as it is. Throws a <see cref="BookingTemplateValidationException"/> if the template is invalid, or a
    /// <see cref="BookingTemplateNotFoundException"/> if no template with its ID is stored.
    /// </summary>
    Task UpdateBookingTemplateAsync(BookingTemplate bookingTemplate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves the booking template with the given ID one place up or down by swapping places with its neighbor; the
    /// positions of all templates are numbered afresh in the new order. Does nothing if there is no neighbor in that
    /// direction (the template is already first or last). Throws a
    /// <see cref="BookingTemplateNotFoundException"/> if no template with the given ID is stored.
    /// </summary>
    Task MoveBookingTemplateAsync(
        Guid bookingTemplateId,
        BookingTemplateMoveDirection direction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all stored booking templates in the order they are offered in, i.e. ascending by position. The list is
    /// empty if no templates are stored.
    /// </summary>
    Task<List<BookingTemplate>> GetAllBookingTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the booking template with the given ID. Does nothing if no such template is stored.
    /// </summary>
    Task DeleteBookingTemplateAsync(Guid bookingTemplateId, CancellationToken cancellationToken = default);
}
