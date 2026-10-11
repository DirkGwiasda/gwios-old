namespace GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Exceptions;

/// <summary>
/// Thrown when an operation requires a stored booking template, but no booking template with the given ID is stored.
/// </summary>
/// <param name="bookingTemplateId">The ID that no stored booking template has.</param>
public sealed class BookingTemplateNotFoundException(Guid bookingTemplateId)
    : Exception($"No booking template with ID '{bookingTemplateId}' is stored.")
{
    /// <summary>
    /// The ID that no stored booking template has.
    /// </summary>
    public Guid BookingTemplateId { get; } = bookingTemplateId;
}
