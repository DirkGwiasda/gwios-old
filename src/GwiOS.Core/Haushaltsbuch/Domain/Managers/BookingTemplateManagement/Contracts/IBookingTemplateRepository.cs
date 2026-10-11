using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Exceptions;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Models;

namespace GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts;

/// <summary>
/// Persists, reads and deletes Haushaltsbuch booking templates in the underlying storage.
/// </summary>
public interface IBookingTemplateRepository
{
    /// <summary>
    /// Creates the tables required for booking templates unless they already exist. The database itself and the
    /// category tables must already exist. The operation is idempotent: calling it against existing tables changes
    /// nothing.
    /// </summary>
    Task EnsureStorageCreatedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores the given new booking template, including its <see cref="BookingTemplate.Position"/>.
    /// </summary>
    Task InsertAsync(BookingTemplate bookingTemplate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Overwrites the description and category of the stored booking template with the ID of the given template; its
    /// stored position stays as it is. Throws a <see cref="BookingTemplateNotFoundException"/> if no such template is
    /// stored.
    /// </summary>
    Task UpdateAsync(BookingTemplate bookingTemplate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Overwrites the stored positions of all given booking templates at once: either all of them change or, if one
    /// is not stored, none. Throws a <see cref="BookingTemplateNotFoundException"/> in that case.
    /// </summary>
    Task UpdatePositionsAsync(
        IReadOnlyList<BookingTemplate> bookingTemplates,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all stored booking templates, ordered ascending by position and, among equal positions, by ID. The list
    /// is empty if no booking templates are stored.
    /// </summary>
    Task<List<BookingTemplate>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the booking template with the given ID. Does nothing if no such template is stored.
    /// </summary>
    Task DeleteAsync(Guid bookingTemplateId, CancellationToken cancellationToken = default);
}
